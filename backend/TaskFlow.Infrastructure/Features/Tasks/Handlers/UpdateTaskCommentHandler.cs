using System.Text.Encodings.Web;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class UpdateTaskCommentHandler(
    ITaskRepository taskRepository,
    ICurrentUser currentUser,
    IMemoryCache cache) : IRequestHandler<UpdateTaskCommentCommand, UpdateTaskCommentResult>
{
    public async System.Threading.Tasks.Task<UpdateTaskCommentResult> Handle(
        UpdateTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var result = await taskRepository.UpdateTaskCommentAsync(
            request.TaskId,
            request.CommentId,
            currentUser.UserId,
            request.Content,
            cancellationToken);
        if (result.Comment is null)
        {
            var detail = result.StatusCode == StatusCodes.Status403Forbidden
                ? "You can only edit your own comments."
                : result.StatusCode == StatusCodes.Status400BadRequest
                    ? "Content is too long after encoding."
                    : null;
            return new UpdateTaskCommentResult(null, result.StatusCode, detail);
        }

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, result.AssigneeId);

        var author = result.Comment.AuthorId is { } aid
            ? new TaskAssigneeDto(aid, result.Comment.AuthorUserName ?? string.Empty, result.Comment.AuthorDisplayName)
            : null;

        return new UpdateTaskCommentResult(new CommentDto(
            result.Comment.Id,
            result.Comment.Content,
            result.Comment.IsEdited,
            new DateTimeOffset(result.Comment.CreatedAtUtc, TimeSpan.Zero),
            new DateTimeOffset(result.Comment.UpdatedAtUtc, TimeSpan.Zero),
            author,
            result.Comment.IsDeleted), StatusCodes.Status200OK, null);
    }
}
