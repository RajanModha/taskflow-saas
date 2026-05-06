using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Dashboard.Handlers;

public sealed class GetDashboardStatsHandler(
    IDashboardReadRepository dashboardReadRepository,
    ICurrentTenant currentTenant,
    IMemoryCache cache,
    TimeProvider timeProvider)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async System.Threading.Tasks.Task<DashboardStatsDto> Handle(
        GetDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        var orgId = currentTenant.OrganizationId;
        var cacheKey = DashboardCacheKeys.DashboardStats(orgId);

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            return await ComputeAsync(orgId, cancellationToken);
        }) ?? throw new InvalidOperationException("Failed to compute dashboard stats.");
    }

    private async System.Threading.Tasks.Task<DashboardStatsDto> ComputeAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var dueSoonEnd = now.AddDays(7);
        var last7Start = now.AddDays(-7);
        var prev7Start = now.AddDays(-14);

        var pairs = await dashboardReadRepository.GetStatusPriorityCountsAsync(cancellationToken);

        var countsByStatus = new Dictionary<DomainTaskStatus, int>();
        var countsByPriority = new Dictionary<TaskPriority, int>();
        foreach (var p in pairs)
        {
            countsByStatus[p.Status] = countsByStatus.GetValueOrDefault(p.Status) + p.Count;
            countsByPriority[p.Priority] = countsByPriority.GetValueOrDefault(p.Priority) + p.Count;
        }

        var statuses = (DomainTaskStatus[])Enum.GetValues(typeof(DomainTaskStatus));
        var priorities = (TaskPriority[])Enum.GetValues(typeof(TaskPriority));

        var totalTasks = countsByStatus.Values.Sum();
        var completedTasks = countsByStatus.GetValueOrDefault(DomainTaskStatus.Done);
        var cancelledTasks = countsByStatus.GetValueOrDefault(DomainTaskStatus.Cancelled);
        var inProgressTasks = countsByStatus.GetValueOrDefault(DomainTaskStatus.InProgress);
        var pendingTasks =
            countsByStatus.GetValueOrDefault(DomainTaskStatus.Backlog)
            + countsByStatus.GetValueOrDefault(DomainTaskStatus.Todo)
            + countsByStatus.GetValueOrDefault(DomainTaskStatus.InProgress);

        var tasksByStatus = statuses
            .OrderBy(s => (int)s)
            .Select(s => new TasksByStatusDto(s.ToString(), countsByStatus.GetValueOrDefault(s)))
            .ToList();

        var tasksByPriority = priorities
            .OrderBy(p => (int)p)
            .Select(p => new TasksByPriorityDto(p.ToString(), countsByPriority.GetValueOrDefault(p)))
            .ToList();

        var overdueCount = await dashboardReadRepository.GetOverdueCountAsync(now, cancellationToken);
        var dueSoonCount = await dashboardReadRepository.GetDueSoonCountAsync(now, dueSoonEnd, cancellationToken);

        var completionRate = totalTasks == 0
            ? 0m
            : Math.Round((decimal)completedTasks * 100m / totalTasks, 1, MidpointRounding.AwayFromZero);

        var completedLast7Days = await dashboardReadRepository.GetCompletedCountInRangeAsync(last7Start, now, cancellationToken);
        var completedPrev7Days = await dashboardReadRepository.GetCompletedCountInRangeAsync(prev7Start, last7Start, cancellationToken);

        var trendPercent = completedPrev7Days == 0
            ? (completedLast7Days > 0 ? 100m : 0m)
            : Math.Round(
                (decimal)(completedLast7Days - completedPrev7Days) * 100m / completedPrev7Days,
                1,
                MidpointRounding.AwayFromZero);

        var velocity = new DashboardVelocityDto(completedLast7Days, completedPrev7Days, trendPercent);

        var upcomingRaw = await dashboardReadRepository.GetUpcomingTasksAsync(now, 5, cancellationToken);
        var upcomingTasks = upcomingRaw.Select(row =>
        {
            TaskAssigneeDto? assigneeDto = row.AssigneeId is { } aid
                ? new TaskAssigneeDto(aid, row.AssigneeUserName ?? string.Empty, row.AssigneeDisplayName)
                : null;
            return new DashboardUpcomingTaskDto(
                row.Id,
                row.Title,
                row.ProjectId,
                row.ProjectName,
                row.DueDateUtc,
                row.Priority,
                assigneeDto);
        }).ToList();

        var recentRaw = await dashboardReadRepository.GetRecentActivityAsync(10, cancellationToken);
        var recentActivity = recentRaw
            .Select(a => new DashboardRecentActivityDto(a.Action, a.ActorName, a.OccurredAt, a.EntityTitle))
            .ToList();

        var projectAgg = await dashboardReadRepository.GetProjectSummariesAsync(now, cancellationToken);

        var projectSummaries = projectAgg
            .OrderBy(p => p.ProjectName)
            .Select(
                p => new DashboardProjectSummaryDto(
                    p.ProjectId,
                    p.ProjectName,
                    p.TotalTasks,
                    p.CompletedTasks,
                    p.OverdueCount,
                    p.TotalTasks == 0
                        ? 0m
                        : Math.Round((decimal)p.CompletedTasks * 100m / p.TotalTasks, 1, MidpointRounding.AwayFromZero)))
            .ToList();

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var contributors = await dashboardReadRepository.GetTopContributorsAsync(
            organizationId,
            monthStart,
            monthEnd,
            ActivityActions.TaskStatusChanged,
            cancellationToken);
        var topContributors = contributors
            .Select(r => new DashboardTopContributorDto(r.UserId, r.UserName, r.DisplayName, r.TasksCompleted))
            .ToList();

        return new DashboardStatsDto(
            totalTasks,
            completedTasks,
            pendingTasks,
            tasksByStatus,
            inProgressTasks,
            cancelledTasks,
            tasksByPriority,
            overdueCount,
            dueSoonCount,
            completionRate,
            velocity,
            upcomingTasks,
            recentActivity,
            projectSummaries,
            topContributors);
    }
}
