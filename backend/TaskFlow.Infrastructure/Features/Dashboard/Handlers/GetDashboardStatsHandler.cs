using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Dashboard.Handlers;

internal sealed class ContributorAggRow
{
    public Guid ActorId { get; set; }

    public int TasksCompleted { get; set; }
}

public sealed class GetDashboardStatsHandler(
    TaskFlowDbContext dbContext,
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

        var pairs = new List<StatusPriorityCountRow>(32);
        await foreach (var row in DashboardCompiledQueries.StatusPriorityGroups(dbContext))
        {
            pairs.Add(row);
        }

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

        var overdueCount = await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.DueDateUtc != null
                 && t.DueDateUtc < now
                 && t.Status != DomainTaskStatus.Done
                 && t.Status != DomainTaskStatus.Cancelled,
            cancellationToken);

        var dueSoonCount = await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.DueDateUtc != null
                 && t.DueDateUtc >= now
                 && t.DueDateUtc <= dueSoonEnd
                 && t.Status != DomainTaskStatus.Done
                 && t.Status != DomainTaskStatus.Cancelled,
            cancellationToken);

        var completionRate = totalTasks == 0
            ? 0m
            : Math.Round((decimal)completedTasks * 100m / totalTasks, 1, MidpointRounding.AwayFromZero);

        var completedLast7Days = await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.Status == DomainTaskStatus.Done
                 && t.UpdatedAtUtc >= last7Start
                 && t.UpdatedAtUtc < now,
            cancellationToken);

        var completedPrev7Days = await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.Status == DomainTaskStatus.Done
                 && t.UpdatedAtUtc >= prev7Start
                 && t.UpdatedAtUtc < last7Start,
            cancellationToken);

        var trendPercent = completedPrev7Days == 0
            ? (completedLast7Days > 0 ? 100m : 0m)
            : Math.Round(
                (decimal)(completedLast7Days - completedPrev7Days) * 100m / completedPrev7Days,
                1,
                MidpointRounding.AwayFromZero);

        var velocity = new DashboardVelocityDto(completedLast7Days, completedPrev7Days, trendPercent);

        var upcomingRaw = await (
                from t in dbContext.Tasks.AsNoTracking()
                join p in dbContext.Projects.AsNoTracking() on t.ProjectId equals p.Id
                where t.Status != DomainTaskStatus.Done
                      && t.Status != DomainTaskStatus.Cancelled
                      && t.DueDateUtc != null
                      && t.DueDateUtc >= now
                orderby t.DueDateUtc
                select new { Task = t, ProjectName = p.Name })
            .Take(5)
            .ToListAsync(cancellationToken);

        var assigneeIds = upcomingRaw
            .Where(x => x.Task.AssigneeId is not null)
            .Select(x => x.Task.AssigneeId!.Value)
            .Distinct()
            .ToList();

        var assignees = await dbContext.Users.AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var upcomingTasks = new List<DashboardUpcomingTaskDto>(upcomingRaw.Count);
        foreach (var row in upcomingRaw)
        {
            TaskAssigneeDto? assigneeDto = null;
            if (row.Task.AssigneeId is { } aid && assignees.TryGetValue(aid, out var user))
            {
                assigneeDto = new TaskAssigneeDto(user.Id, user.UserName ?? string.Empty, user.DisplayName);
            }

            upcomingTasks.Add(
                new DashboardUpcomingTaskDto(
                    row.Task.Id,
                    row.Task.Title,
                    row.Task.ProjectId,
                    row.ProjectName,
                    row.Task.DueDateUtc,
                    row.Task.Priority,
                    assigneeDto));
        }

        var recentLogs = await dbContext.ActivityLogs.AsNoTracking()
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentActivity =
            await DashboardActivityEnrichment.ToRecentActivityDtosAsync(dbContext, recentLogs, cancellationToken);

        var projectAgg = await (
                from t in dbContext.Tasks.AsNoTracking()
                join p in dbContext.Projects.AsNoTracking() on t.ProjectId equals p.Id
                group t by new { p.Id, p.Name }
                into g
                select new
                {
                    g.Key.Id,
                    g.Key.Name,
                    Total = g.Count(),
                    Completed = g.Count(x => x.Status == DomainTaskStatus.Done),
                    Overdue = g.Count(x =>
                        x.DueDateUtc != null
                        && x.DueDateUtc < now
                        && x.Status != DomainTaskStatus.Done
                        && x.Status != DomainTaskStatus.Cancelled),
                })
            .ToListAsync(cancellationToken);

        var projectSummaries = projectAgg
            .OrderBy(p => p.Name)
            .Select(
                p => new DashboardProjectSummaryDto(
                    p.Id,
                    p.Name,
                    p.Total,
                    p.Completed,
                    p.Overdue,
                    p.Total == 0
                        ? 0m
                        : Math.Round((decimal)p.Completed * 100m / p.Total, 1, MidpointRounding.AwayFromZero)))
            .ToList();

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var contributorRows = await dbContext.Database
            .SqlQuery<ContributorAggRow>(
                $"""
                 SELECT a."ActorId" AS "{nameof(ContributorAggRow.ActorId)}", COUNT(*)::int AS "{nameof(ContributorAggRow.TasksCompleted)}"
                 FROM "ActivityLogs" AS a
                 WHERE a."OrganizationId" = {organizationId}
                   AND a."Action" = {ActivityActions.TaskStatusChanged}
                   AND a."Metadata" IS NOT NULL
                   AND (a."Metadata"::jsonb ->> 'to') = 'Done'
                   AND a."OccurredAtUtc" >= {monthStart}
                   AND a."OccurredAtUtc" < {monthEnd}
                 GROUP BY a."ActorId"
                 ORDER BY COUNT(*) DESC
                 LIMIT 5
                 """)
            .ToListAsync(cancellationToken);

        var contributorIds = contributorRows.Select(r => r.ActorId).ToList();
        var contributorUsers = await dbContext.Users.AsNoTracking()
            .Where(u => contributorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var topContributors = contributorRows
            .Select(
                r => contributorUsers.TryGetValue(r.ActorId, out var u)
                    ? new DashboardTopContributorDto(u.Id, u.UserName ?? string.Empty, u.DisplayName, r.TasksCompleted)
                    : new DashboardTopContributorDto(r.ActorId, string.Empty, null, r.TasksCompleted))
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
