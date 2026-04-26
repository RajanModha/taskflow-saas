using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class DeleteTaskCommentHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IBoardCacheVersion boardCacheVersion) : IRequestHandler<DeleteTaskCommentCommand, DeleteTaskCommentResult>
{
    public async System.Threading.Tasks.Task<DeleteTaskCommentResult> Handle(
        DeleteTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(
            dbContext,
            currentTenant,
            request.TaskId,
            cancellationToken);

        if (task is null)
        {
            return new DeleteTaskCommentResult(StatusCodes.Status404NotFound);
        }

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(
                c => c.Id == request.CommentId && c.TaskId == request.TaskId,
                cancellationToken);

        if (comment is null)
        {
            return new DeleteTaskCommentResult(StatusCodes.Status404NotFound);
        }

        if (currentUser.UserId is not { } userId)
        {
            return new DeleteTaskCommentResult(StatusCodes.Status403Forbidden);
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return new DeleteTaskCommentResult(StatusCodes.Status403Forbidden);
        }

        var isAuthor = comment.AuthorId == userId;
        var isPrivileged = user.WorkspaceRole is WorkspaceRole.Owner or WorkspaceRole.Admin;
        if (!isAuthor && !isPrivileged)
        {
            return new DeleteTaskCommentResult(StatusCodes.Status403Forbidden);
        }

        if (comment.IsDeleted)
        {
            return new DeleteTaskCommentResult(StatusCodes.Status204NoContent);
        }

        comment.IsDeleted = true;
        comment.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);

        boardCacheVersion.BumpProject(task.ProjectId);

        return new DeleteTaskCommentResult(StatusCodes.Status204NoContent);
    }
}
