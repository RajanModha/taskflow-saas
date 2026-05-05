using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Tasks;
using DomainTask = TaskFlow.Domain.Entities.Task;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class TaskRepository(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    ICurrentTenant currentTenant) : ITaskRepository
{
    public Task<long> GetExportCountAsync(TaskExportFilters filters, CancellationToken cancellationToken) =>
        BuildFilteredQuery(filters).LongCountAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetExportAssigneeDisplayNamesAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken)
    {
        var assigneeIds = await BuildFilteredQuery(filters)
            .Where(t => t.AssigneeId != null)
            .Select(t => t.AssigneeId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (assigneeIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => u.DisplayName ?? u.UserName ?? u.Email ?? string.Empty,
                cancellationToken);
    }

    public async IAsyncEnumerable<DomainTask> GetExportStreamAsync(
        TaskExportFilters filters,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = ApplySort(BuildFilteredQuery(filters), filters)
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
            .AsSplitQuery();

        await foreach (var task in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return task;
        }
    }

    public async System.Threading.Tasks.Task<PagedResult<DomainTask>> GetPagedTasksAsync(
        TaskListCriteria criteria,
        CancellationToken cancellationToken)
    {
        var page = criteria.Page < 1 ? 1 : criteria.Page;
        var pageSize = criteria.PageSize is < 1 or > 100 ? 20 : criteria.PageSize;
        if (!currentTenant.IsSet || criteria.ForceEmptyResult)
        {
            return new PagedResult<DomainTask>([], page, pageSize, 0);
        }

        var skip = (page - 1) * pageSize;

        var query = dbContext.Tasks.AsNoTracking().AsQueryable();
        if (criteria.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (criteria.DeletedOnly)
        {
            query = query.Where(t => t.IsDeleted);
        }

        if (criteria.ProjectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == criteria.ProjectId.Value);
        }

        if (criteria.Status.HasValue)
        {
            query = query.Where(t => t.Status == criteria.Status.Value);
        }

        if (criteria.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == criteria.Priority.Value);
        }

        if (criteria.DueFromUtc.HasValue)
        {
            query = query.Where(t => t.DueDateUtc.HasValue && t.DueDateUtc.Value >= criteria.DueFromUtc.Value);
        }

        if (criteria.DueToUtc.HasValue)
        {
            query = query.Where(t => t.DueDateUtc.HasValue && t.DueDateUtc.Value <= criteria.DueToUtc.Value);
        }

        if (criteria.AssigneeId.HasValue)
        {
            query = query.Where(t => t.AssigneeId == criteria.AssigneeId.Value);
        }

        if (criteria.TagId.HasValue)
        {
            var tagId = criteria.TagId.Value;
            query = query.Where(t => dbContext.TaskTags.Any(tt => tt.TaskId == t.Id && tt.TagId == tagId));
        }

        if (criteria.MilestoneId.HasValue)
        {
            query = query.Where(t => t.MilestoneId == criteria.MilestoneId.Value);
        }

        if (criteria.IsBlocked == true)
        {
            query = query.Where(t =>
                dbContext.TaskDependencies.Any(d =>
                    d.BlockedTaskId == t.Id &&
                    dbContext.Tasks.Any(b =>
                        b.Id == d.BlockingTaskId
                        && b.Status != DomainTaskStatus.Done
                        && b.Status != DomainTaskStatus.Cancelled)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Q))
        {
            var q = criteria.Q.Trim();
            query = query.Where(t => t.Title.Contains(q));
        }

        var sortBy = criteria.SortBy?.Trim().ToLowerInvariant();

        query = sortBy switch
        {
            "duedateutc" => criteria.SortDesc
                ? query.OrderByDescending(t => t.DueDateUtc ?? DateTime.MaxValue)
                : query.OrderBy(t => t.DueDateUtc ?? DateTime.MaxValue),
            "priority" => criteria.SortDesc
                ? query.OrderByDescending(t => (int)t.Priority)
                : query.OrderBy(t => (int)t.Priority),
            "status" => criteria.SortDesc
                ? query.OrderByDescending(t => (int)t.Status)
                : query.OrderBy(t => (int)t.Status),
            "createdatutc" or null or "" => criteria.SortDesc
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc),
            _ => criteria.SortDesc
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc),
        };

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<DomainTask>(items, page, pageSize, total);
    }

    public async System.Threading.Tasks.Task<DomainTask?> GetDetachedTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken) =>
        await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);

    public async System.Threading.Tasks.Task<PagedResult<DomainTask>> GetPagedOverdueTasksAsync(
        OverdueTaskListCriteria criteria,
        CancellationToken cancellationToken)
    {
        var page = criteria.Page < 1 ? 1 : criteria.Page;
        var pageSize = criteria.PageSize is < 1 or > 100 ? 20 : criteria.PageSize;
        if (!currentTenant.IsSet)
        {
            return new PagedResult<DomainTask>([], page, pageSize, 0);
        }

        var skip = (page - 1) * pageSize;
        var now = DateTime.UtcNow;

        var query = dbContext.Tasks
            .AsNoTracking()
            .Where(t =>
                t.DueDateUtc.HasValue &&
                t.DueDateUtc < now &&
                t.Status != DomainTaskStatus.Done &&
                t.Status != DomainTaskStatus.Cancelled)
            .OrderBy(t => t.DueDateUtc);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<DomainTask>(items, page, pageSize, total);
    }

    public async System.Threading.Tasks.Task<PagedResult<TaskCommentReadModel>?> GetPagedTaskCommentsAsync(
        Guid taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var skip = (page - 1) * pageSize;

        var baseQuery = dbContext.Comments
            .AsNoTracking()
            .Where(c => c.TaskId == taskId);

        var total = await baseQuery.LongCountAsync(cancellationToken);
        var rows = await baseQuery
            .OrderBy(c => c.CreatedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var authorIds = rows.Where(c => !c.IsDeleted).Select(c => c.AuthorId).Distinct().ToList();
        var authors = authorIds.Count == 0
            ? new Dictionary<Guid, (string? UserName, string? DisplayName)>()
            : await dbContext.Users
                .AsNoTracking()
                .Where(u => authorIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => (u.UserName, u.DisplayName),
                    cancellationToken);

        var items = rows
            .Select(c =>
            {
                if (c.IsDeleted || !authors.TryGetValue(c.AuthorId, out var author))
                {
                    return new TaskCommentReadModel(
                        c.Id,
                        c.Content,
                        c.IsEdited,
                        c.CreatedAtUtc,
                        c.UpdatedAtUtc,
                        c.IsDeleted,
                        null,
                        null,
                        null);
                }

                return new TaskCommentReadModel(
                    c.Id,
                    c.Content,
                    c.IsEdited,
                    c.CreatedAtUtc,
                    c.UpdatedAtUtc,
                    c.IsDeleted,
                    c.AuthorId,
                    author.UserName,
                    author.DisplayName);
            })
            .ToList();

        return new PagedResult<TaskCommentReadModel>(items, page, pageSize, total);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<TaskChecklistItemReadModel>?> GetTaskChecklistAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var rows = await dbContext.ChecklistItems
            .AsNoTracking()
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);

        return rows.Select(c => new TaskChecklistItemReadModel(
                c.Id,
                c.Title,
                c.IsCompleted,
                c.Order,
                c.CompletedAtUtc))
            .ToList();
    }

    public async System.Threading.Tasks.Task<PagedResult<TaskFlow.Domain.Entities.ActivityLog>?> GetPagedTaskActivityAsync(
        Guid taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var skip = (page - 1) * pageSize;

        var query = dbContext.ActivityLogs
            .AsNoTracking()
            .Where(a => a.EntityType == ActivityEntityTypes.Task && a.EntityId == taskId);

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskFlow.Domain.Entities.ActivityLog>(rows, page, pageSize, total);
    }

    public async System.Threading.Tasks.Task<TaskDependenciesReadModel?> GetTaskDependenciesAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var blockedByRows = await (
                from d in dbContext.TaskDependencies.AsNoTracking()
                join b in dbContext.Tasks.AsNoTracking() on d.BlockingTaskId equals b.Id
                where d.BlockedTaskId == taskId
                select new TaskBlockingSummaryReadModel(b.Id, b.Title, b.Status))
            .ToListAsync(cancellationToken);

        var blockedIds = await dbContext.TaskDependencies
            .AsNoTracking()
            .Where(d => d.BlockingTaskId == taskId)
            .Select(d => d.BlockedTaskId)
            .ToListAsync(cancellationToken);

        var blockingRows = await dbContext.Tasks
            .AsNoTracking()
            .Where(t => blockedIds.Contains(t.Id))
            .Select(t => new TaskBlockingSummaryReadModel(t.Id, t.Title, t.Status))
            .ToListAsync(cancellationToken);

        return new TaskDependenciesReadModel(task.Id, task.Title, task.Status, blockedByRows, blockingRows);
    }

    private IQueryable<DomainTask> BuildFilteredQuery(TaskExportFilters filters)
    {
        var query = dbContext.Tasks.AsQueryable();
        if (filters.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (filters.ProjectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == filters.ProjectId.Value);
        }

        if (filters.Status.HasValue)
        {
            query = query.Where(t => t.Status == filters.Status.Value);
        }

        if (filters.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filters.Priority.Value);
        }

        if (filters.DueFromUtc.HasValue)
        {
            query = query.Where(t => t.DueDateUtc.HasValue && t.DueDateUtc.Value >= filters.DueFromUtc.Value);
        }

        if (filters.DueToUtc.HasValue)
        {
            query = query.Where(t => t.DueDateUtc.HasValue && t.DueDateUtc.Value <= filters.DueToUtc.Value);
        }

        if (filters.AssignedToMe == true)
        {
            if (currentUser.UserId is { } me)
            {
                query = query.Where(t => t.AssigneeId == me);
            }
            else
            {
                query = query.Where(_ => false);
            }
        }

        if (filters.AssigneeId.HasValue)
        {
            query = query.Where(t => t.AssigneeId == filters.AssigneeId.Value);
        }

        if (filters.TagId.HasValue)
        {
            var tagId = filters.TagId.Value;
            query = query.Where(t => dbContext.TaskTags.Any(tt => tt.TaskId == t.Id && tt.TagId == tagId));
        }

        if (!string.IsNullOrWhiteSpace(filters.Q))
        {
            var q = filters.Q.Trim();
            query = query.Where(t => t.Title.Contains(q));
        }

        return query;
    }

    private static IQueryable<DomainTask> ApplySort(IQueryable<DomainTask> query, TaskExportFilters filters)
    {
        var sortBy = filters.SortBy?.Trim().ToLowerInvariant();

        return sortBy switch
        {
            "duedateutc" => filters.SortDesc
                ? query.OrderByDescending(t => t.DueDateUtc ?? DateTime.MaxValue)
                : query.OrderBy(t => t.DueDateUtc ?? DateTime.MaxValue),
            "priority" => filters.SortDesc
                ? query.OrderByDescending(t => (int)t.Priority)
                : query.OrderBy(t => (int)t.Priority),
            "status" => filters.SortDesc
                ? query.OrderByDescending(t => (int)t.Status)
                : query.OrderBy(t => (int)t.Status),
            "createdatutc" or null or "" => filters.SortDesc
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc),
            _ => filters.SortDesc
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc),
        };
    }
}
