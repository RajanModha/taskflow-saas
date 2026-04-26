using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class DeleteChecklistItemHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    IMemoryCache cache)
    : IRequestHandler<DeleteChecklistItemCommand, bool>
{
    public async Task<bool> Handle(DeleteChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(
            dbContext,
            currentTenant,
            request.TaskId,
            cancellationToken);

        if (task is null)
        {
            return false;
        }

        var deleted = await dbContext.ChecklistItems
            .Where(c => c.TaskId == request.TaskId && c.Id == request.ItemId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            return false;
        }

        boardCacheVersion.BumpProject(task.ProjectId);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, task.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, task.AssigneeId);

        return true;
    }
}
