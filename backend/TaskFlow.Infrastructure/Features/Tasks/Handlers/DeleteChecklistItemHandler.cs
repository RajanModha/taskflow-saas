using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class DeleteChecklistItemHandler(
    ITaskChecklistRepository taskRepository,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    IMemoryCache cache)
    : IRequestHandler<DeleteChecklistItemCommand, bool>
{
    public async Task<bool> Handle(DeleteChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.DeleteChecklistItemAsync(
            request.TaskId,
            request.ItemId,
            cancellationToken);
        if (result is null || !result.Deleted)
        {
            return false;
        }
        boardCacheVersion.BumpProject(result.ProjectId);
        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, result.AssigneeId);

        return true;
    }
}
