using Dapper;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Activity;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence.Sql;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Persistence;

internal sealed class ContributorAggRow
{
    public Guid ActorId { get; set; }
    public int TasksCompleted { get; set; }
}

public sealed class DashboardReadRepository(TaskFlowDbContext dbContext) : IDashboardReadRepository
{
    public async Task<IReadOnlyList<DashboardStatusPriorityCountReadModel>> GetStatusPriorityCountsAsync(CancellationToken cancellationToken) =>
        await dbContext.Tasks.AsNoTracking()
            .GroupBy(t => new { t.Status, t.Priority })
            .Select(g => new DashboardStatusPriorityCountReadModel(g.Key.Status, g.Key.Priority, g.Count()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DashboardStatusPriorityCountReadModel>> GetMyStatusPriorityCountsAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Tasks.AsNoTracking()
            .Where(t => t.AssigneeId == userId)
            .GroupBy(t => new { t.Status, t.Priority })
            .Select(g => new DashboardStatusPriorityCountReadModel(g.Key.Status, g.Key.Priority, g.Count()))
            .ToListAsync(cancellationToken);

    public async Task<int> GetOverdueCountAsync(DateTime nowUtc, CancellationToken cancellationToken) =>
        await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.DueDateUtc != null
                 && t.DueDateUtc < nowUtc
                 && t.Status != DomainTaskStatus.Done
                 && t.Status != DomainTaskStatus.Cancelled,
            cancellationToken);

    public async Task<int> GetMyOverdueCountAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken) =>
        await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.AssigneeId == userId
                 && t.DueDateUtc != null
                 && t.DueDateUtc < nowUtc
                 && t.Status != DomainTaskStatus.Done
                 && t.Status != DomainTaskStatus.Cancelled,
            cancellationToken);

    public async Task<int> GetDueSoonCountAsync(DateTime nowUtc, DateTime dueSoonEndUtc, CancellationToken cancellationToken) =>
        await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.DueDateUtc != null
                 && t.DueDateUtc >= nowUtc
                 && t.DueDateUtc <= dueSoonEndUtc
                 && t.Status != DomainTaskStatus.Done
                 && t.Status != DomainTaskStatus.Cancelled,
            cancellationToken);

    public async Task<int> GetMyDueSoonCountAsync(Guid userId, DateTime nowUtc, DateTime dueSoonEndUtc, CancellationToken cancellationToken) =>
        await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.AssigneeId == userId
                 && t.DueDateUtc != null
                 && t.DueDateUtc >= nowUtc
                 && t.DueDateUtc <= dueSoonEndUtc
                 && t.Status != DomainTaskStatus.Done
                 && t.Status != DomainTaskStatus.Cancelled,
            cancellationToken);

    public async Task<int> GetCompletedCountInRangeAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken) =>
        await dbContext.Tasks.AsNoTracking().CountAsync(
            t => t.Status == DomainTaskStatus.Done && t.UpdatedAtUtc >= startUtc && t.UpdatedAtUtc < endUtc,
            cancellationToken);

    public async Task<IReadOnlyList<DashboardUpcomingTaskReadModel>> GetUpcomingTasksAsync(
        DateTime nowUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        var raw = await (
                from t in dbContext.Tasks.AsNoTracking()
                join p in dbContext.Projects.AsNoTracking() on t.ProjectId equals p.Id
                where t.Status != DomainTaskStatus.Done
                      && t.Status != DomainTaskStatus.Cancelled
                      && t.DueDateUtc != null
                      && t.DueDateUtc >= nowUtc
                orderby t.DueDateUtc
                select new { Task = t, ProjectName = p.Name })
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (raw.Count == 0)
        {
            return [];
        }

        var assigneeIds = raw.Where(x => x.Task.AssigneeId != null).Select(x => x.Task.AssigneeId!.Value).Distinct().ToList();
        var assignees = await dbContext.Users.AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return raw.Select(x =>
        {
            assignees.TryGetValue(x.Task.AssigneeId ?? Guid.Empty, out var u);
            return new DashboardUpcomingTaskReadModel(
                x.Task.Id,
                x.Task.Title,
                x.Task.ProjectId,
                x.ProjectName,
                x.Task.DueDateUtc,
                x.Task.Priority,
                x.Task.AssigneeId,
                u?.UserName,
                u?.DisplayName);
        }).ToList();
    }

    public async Task<IReadOnlyList<DashboardProjectSummaryReadModel>> GetProjectSummariesAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rows = await (
                from t in dbContext.Tasks.AsNoTracking()
                join p in dbContext.Projects.AsNoTracking() on t.ProjectId equals p.Id
                group t by new { p.Id, p.Name }
                into g
                select new DashboardProjectSummaryReadModel(
                    g.Key.Id,
                    g.Key.Name,
                    g.Count(),
                    g.Count(x => x.Status == DomainTaskStatus.Done),
                    g.Count(x => x.DueDateUtc != null
                                 && x.DueDateUtc < nowUtc
                                 && x.Status != DomainTaskStatus.Done
                                 && x.Status != DomainTaskStatus.Cancelled)))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<DashboardTopContributorReadModel>> GetTopContributorsAsync(
        Guid organizationId,
        DateTime monthStartUtc,
        DateTime monthEndUtc,
        string statusChangedAction,
        CancellationToken cancellationToken)
    {
        var sql = RawSqlQueryProvider.GetByKey("dashboard.top_contributors.monthly");
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var contributorRows = (await connection.QueryAsync<ContributorAggRow>(
            new CommandDefinition(
                sql,
                new
                {
                    OrganizationId = organizationId,
                    TaskStatusChangedAction = statusChangedAction,
                    MonthStart = monthStartUtc,
                    MonthEnd = monthEndUtc
                },
                cancellationToken: cancellationToken))).ToList();

        var ids = contributorRows.Select(r => r.ActorId).ToList();
        var users = await dbContext.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return contributorRows
            .Select(r => users.TryGetValue(r.ActorId, out var u)
                ? new DashboardTopContributorReadModel(u.Id, u.UserName ?? string.Empty, u.DisplayName, r.TasksCompleted)
                : new DashboardTopContributorReadModel(r.ActorId, string.Empty, null, r.TasksCompleted))
            .ToList();
    }

    public async Task<IReadOnlyList<DashboardRecentActivityReadModel>> GetRecentActivityAsync(int limit, CancellationToken cancellationToken)
    {
        var logs = await dbContext.ActivityLogs.AsNoTracking()
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return await MapRecentActivityAsync(logs, cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardRecentActivityReadModel>> GetMyRecentActivityAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        var logs = await dbContext.ActivityLogs.AsNoTracking()
            .Where(a => a.ActorId == userId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return await MapRecentActivityAsync(logs, cancellationToken);
    }

    private async Task<IReadOnlyList<DashboardRecentActivityReadModel>> MapRecentActivityAsync(
        IReadOnlyList<ActivityLog> logs,
        CancellationToken cancellationToken)
    {
        if (logs.Count == 0)
        {
            return [];
        }

        var taskIds = logs.Where(l => l.EntityType == ActivityEntityTypes.Task).Select(l => l.EntityId).Distinct().ToList();
        var projectIds = logs.Where(l => l.EntityType == ActivityEntityTypes.Project).Select(l => l.EntityId).Distinct().ToList();

        var taskTitles = await dbContext.Tasks.AsNoTracking()
            .Where(t => taskIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var projectNames = await dbContext.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var dtos = DashboardActivityEnrichment.ToRecentActivityDtos(logs, taskTitles, projectNames);
        return dtos
            .Select(d => new DashboardRecentActivityReadModel(d.Action, d.ActorName, d.OccurredAt, d.EntityTitle))
            .ToList();
    }
}
