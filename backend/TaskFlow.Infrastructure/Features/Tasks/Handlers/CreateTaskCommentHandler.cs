using System.Text.Encodings.Web;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class CreateTaskCommentHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger) : IRequestHandler<CreateTaskCommentCommand, CreateTaskCommentResult>
{
    public async System.Threading.Tasks.Task<CreateTaskCommentResult> Handle(
        CreateTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(
            dbContext,
            currentTenant,
            request.TaskId,
            cancellationToken);

        if (task is null)
        {
            return new CreateTaskCommentResult(null, StatusCodes.Status404NotFound);
        }

        if (currentUser.UserId is not { } authorId)
        {
            return new CreateTaskCommentResult(null, StatusCodes.Status401Unauthorized);
        }

        var encoded = HtmlEncoder.Default.Encode(request.Content.Trim());
        if (encoded.Length > 4000)
        {
            return new CreateTaskCommentResult(null, StatusCodes.Status400BadRequest);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var entity = new TaskFlow.Domain.Entities.Comment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            AuthorId = authorId,
            Content = encoded,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsEdited = false,
            IsDeleted = false,
        };

        await dbContext.Comments.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var author = await dbContext.Users
            .AsNoTracking()
            .FirstAsync(u => u.Id == authorId, cancellationToken);

        await activityLogger.LogAsync(
            ActivityEntityTypes.Task,
            request.TaskId,
            ActivityActions.TaskCommented,
            authorId,
            author.UserName ?? string.Empty,
            task.OrganizationId,
            new { commentId = entity.Id },
            cancellationToken);

        boardCacheVersion.BumpProject(task.ProjectId);

        return new CreateTaskCommentResult(CommentMapper.ToDto(entity, author), StatusCodes.Status201Created);
    }
}
