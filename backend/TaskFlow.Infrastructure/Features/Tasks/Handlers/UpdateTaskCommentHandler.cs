using System.Text.Encodings.Web;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class UpdateTaskCommentHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IMemoryCache cache) : IRequestHandler<UpdateTaskCommentCommand, UpdateTaskCommentResult>
{
    public async System.Threading.Tasks.Task<UpdateTaskCommentResult> Handle(
        UpdateTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(
            dbContext,
            currentTenant,
            request.TaskId,
            cancellationToken);

        if (task is null)
        {
            return new UpdateTaskCommentResult(null, StatusCodes.Status404NotFound, null);
        }

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(
                c => c.Id == request.CommentId && c.TaskId == request.TaskId,
                cancellationToken);

        if (comment is null)
        {
            return new UpdateTaskCommentResult(null, StatusCodes.Status404NotFound, null);
        }

        if (currentUser.UserId != comment.AuthorId)
        {
            return new UpdateTaskCommentResult(
                null,
                StatusCodes.Status403Forbidden,
                "You can only edit your own comments.");
        }

        if (comment.IsDeleted)
        {
            return new UpdateTaskCommentResult(null, StatusCodes.Status404NotFound, null);
        }

        var encoded = HtmlEncoder.Default.Encode(request.Content.Trim());
        if (encoded.Length > 4000)
        {
            return new UpdateTaskCommentResult(null, StatusCodes.Status400BadRequest, "Content is too long after encoding.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        comment.Content = encoded;
        comment.IsEdited = true;
        comment.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, task.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, task.AssigneeId);

        var author = await dbContext.Users
            .AsNoTracking()
            .FirstAsync(u => u.Id == comment.AuthorId, cancellationToken);

        return new UpdateTaskCommentResult(
            CommentMapper.ToDto(comment, author),
            StatusCodes.Status200OK,
            null);
    }
}
