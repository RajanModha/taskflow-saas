using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Dashboard;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tenancy;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Dashboard.Handlers;

public sealed class GetDashboardStatsHandler(TaskFlowDbContext dbContext, ICurrentTenant currentTenant, IMemoryCache cache)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        var cacheKey = $"dashboard_stats:{currentTenant.OrganizationId}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

            // Single grouped query to keep the analytics endpoint cheap.
            var grouped = await dbContext.Tasks
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var totalTasks = grouped.Sum(x => x.Count);
            var completedTasks = grouped
                .Where(x => x.Status == DomainTaskStatus.Done)
                .Sum(x => x.Count);
            var pendingTasks = totalTasks - completedTasks;

            // Always return all statuses (stable ordering for charts and consistent UX).
            var statuses = (DomainTaskStatus[])Enum.GetValues(typeof(DomainTaskStatus));
            var countsByStatus = grouped.ToDictionary(x => x.Status, x => x.Count);

            var tasksByStatus = statuses
                .OrderBy(s => (int)s)
                .Select(s => new TasksByStatusDto(s.ToString(), countsByStatus.TryGetValue(s, out var count) ? count : 0))
                .ToList();

            return new DashboardStatsDto(
                totalTasks,
                completedTasks,
                pendingTasks,
                tasksByStatus);
        }) ?? throw new InvalidOperationException("Failed to compute dashboard stats.");
    }
}

