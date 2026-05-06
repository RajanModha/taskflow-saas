using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Dashboard.Handlers;

public sealed class GetDashboardMyStatsHandler(
    IDashboardReadRepository dashboardReadRepository,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IMemoryCache cache,
    TimeProvider timeProvider)
    : IRequestHandler<GetDashboardMyStatsQuery, DashboardMyStatsDto>
{
    public async System.Threading.Tasks.Task<DashboardMyStatsDto> Handle(
        GetDashboardMyStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        if (currentUser.UserId is not { } userId)
        {
            throw new UnauthorizedAccessException("Authenticated user ID is missing.");
        }

        var cacheKey = DashboardCacheKeys.DashboardMyStats(userId);

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await ComputeAsync(userId, cancellationToken);
        }) ?? throw new InvalidOperationException("Failed to compute personal dashboard stats.");
    }

    private async System.Threading.Tasks.Task<DashboardMyStatsDto> ComputeAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var dueSoonEnd = now.AddDays(7);

        var pairs = await dashboardReadRepository.GetMyStatusPriorityCountsAsync(userId, cancellationToken);

        var countsByStatus = new Dictionary<DomainTaskStatus, int>();
        var countsByPriority = new Dictionary<TaskPriority, int>();
        foreach (var p in pairs)
        {
            countsByStatus[p.Status] = countsByStatus.GetValueOrDefault(p.Status) + p.Count;
            countsByPriority[p.Priority] = countsByPriority.GetValueOrDefault(p.Priority) + p.Count;
        }

        var total = countsByStatus.Values.Sum();
        var completed = countsByStatus.GetValueOrDefault(DomainTaskStatus.Done);
        var overdue = await dashboardReadRepository.GetMyOverdueCountAsync(userId, now, cancellationToken);
        var dueSoon = await dashboardReadRepository.GetMyDueSoonCountAsync(userId, now, dueSoonEnd, cancellationToken);

        var statuses = (DomainTaskStatus[])Enum.GetValues(typeof(DomainTaskStatus));
        var priorities = (TaskPriority[])Enum.GetValues(typeof(TaskPriority));

        var myTasksByStatus = statuses
            .OrderBy(s => (int)s)
            .Select(s => new TasksByStatusDto(s.ToString(), countsByStatus.GetValueOrDefault(s)))
            .ToList();

        var myTasksByPriority = priorities
            .OrderBy(p => (int)p)
            .Select(p => new TasksByPriorityDto(p.ToString(), countsByPriority.GetValueOrDefault(p)))
            .ToList();

        var myRecentRaw = await dashboardReadRepository.GetMyRecentActivityAsync(userId, 5, cancellationToken);
        var myRecentActivity = myRecentRaw
            .Select(a => new DashboardRecentActivityDto(a.Action, a.ActorName, a.OccurredAt, a.EntityTitle))
            .ToList();

        return new DashboardMyStatsDto(
            new MyTasksSummaryDto(total, completed, overdue, dueSoon),
            myTasksByStatus,
            myTasksByPriority,
            myRecentActivity);
    }
}
