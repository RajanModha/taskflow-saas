using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class RemoveTaskTagHandler(
    ITaskRepository taskRepository,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<RemoveTaskTagCommand, int>
{
    public async System.Threading.Tasks.Task<int> Handle(RemoveTaskTagCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.RemoveTaskTagAsync(request.TaskId, request.TagId, cancellationToken);
        if (!result.TaskFound)
        {
            return StatusCodes.Status404NotFound;
        }

        if (result.Changed)
        {
            DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
            DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, result.AssigneeId);
            boardCacheVersion.BumpProject(result.ProjectId);
        }

        return StatusCodes.Status204NoContent;
    }
}
