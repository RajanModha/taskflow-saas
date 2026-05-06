using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class DeleteTaskCommentHandler(
    ITaskCommentRepository taskRepository,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    IMemoryCache cache) : IRequestHandler<DeleteTaskCommentCommand, DeleteTaskCommentResult>
{
    public async System.Threading.Tasks.Task<DeleteTaskCommentResult> Handle(
        DeleteTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var result = await taskRepository.DeleteTaskCommentAsync(
            request.TaskId,
            request.CommentId,
            currentUser.UserId,
            cancellationToken);
        if (result.StatusCode != StatusCodes.Status204NoContent)
        {
            return new DeleteTaskCommentResult(result.StatusCode);
        }
        boardCacheVersion.BumpProject(result.ProjectId);
        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, result.AssigneeId);
        return new DeleteTaskCommentResult(StatusCodes.Status204NoContent);
    }
}
