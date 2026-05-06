using System.Text.Encodings.Web;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class CreateTaskCommentHandler(
    ITaskCommentRepository taskRepository,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    INotificationService notificationService,
    IMemoryCache cache) : IRequestHandler<CreateTaskCommentCommand, CreateTaskCommentResult>
{
    public async System.Threading.Tasks.Task<CreateTaskCommentResult> Handle(
        CreateTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var result = await taskRepository.CreateTaskCommentAsync(
            request.TaskId,
            currentUser.UserId,
            request.Content,
            cancellationToken);
        if (result.StatusCode != StatusCodes.Status201Created || result.Comment is null)
        {
            return new CreateTaskCommentResult(null, result.StatusCode);
        }

        await activityLogger.LogAsync(
            ActivityEntityTypes.Task,
            request.TaskId,
            ActivityActions.TaskCommented,
            currentUser.UserId ?? Guid.Empty,
            string.Empty,
            result.OrganizationId,
            new { commentId = result.Comment.Id },
            cancellationToken);
        if (result.AssigneeId is { } assigneeId &&
            result.Comment.AuthorId is { } authorId &&
            assigneeId != authorId)
        {
            await notificationService.CreateAsync(
                assigneeId,
                "task.commented",
                "New comment",
                "A new comment was added",
                entityType: "Task",
                entityId: request.TaskId,
                ct: cancellationToken);
        }

        boardCacheVersion.BumpProject(result.ProjectId);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, result.AssigneeId);

        var authorDto = result.Comment.AuthorId is { } aid
            ? new TaskAssigneeDto(aid, result.Comment.AuthorUserName ?? string.Empty, result.Comment.AuthorDisplayName)
            : null;
        return new CreateTaskCommentResult(
            new CommentDto(
                result.Comment.Id,
                result.Comment.Content,
                result.Comment.IsEdited,
                new DateTimeOffset(result.Comment.CreatedAtUtc, TimeSpan.Zero),
                new DateTimeOffset(result.Comment.UpdatedAtUtc, TimeSpan.Zero),
                authorDto,
                result.Comment.IsDeleted),
            StatusCodes.Status201Created);
    }
}



