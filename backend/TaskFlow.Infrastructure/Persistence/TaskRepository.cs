using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
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
    ICurrentTenant currentTenant,
    TimeProvider timeProvider) : ITaskRepository
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

    public async System.Threading.Tasks.Task<AssignTaskMutationResult?> AssignTaskAsync(
        Guid taskId,
        Guid? assigneeId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        string? currentAssigneeDisplayName = null;
        string? currentAssigneeUserName = null;
        string? currentAssigneeEmail = null;
        if (assigneeId is { } newAssigneeId)
        {
            var assignee = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == newAssigneeId, cancellationToken);
            if (assignee is null || assignee.OrganizationId != task.OrganizationId)
            {
                return null;
            }

            currentAssigneeDisplayName = assignee.DisplayName ?? assignee.UserName ?? string.Empty;
            currentAssigneeUserName = assignee.UserName;
            currentAssigneeEmail = assignee.Email;
        }

        var previousAssigneeId = task.AssigneeId;
        string? previousAssigneeUserName = null;
        if (previousAssigneeId is { } pId)
        {
            previousAssigneeUserName = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == pId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        task.AssigneeId = assigneeId;
        task.UpdatedAtUtc = DateTime.UtcNow;
        if (previousAssigneeId != assigneeId)
        {
            task.ReminderSent = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var projectName = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == task.ProjectId && p.OrganizationId == task.OrganizationId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new AssignTaskMutationResult(
            task.Id,
            task.Title,
            task.OrganizationId,
            task.ProjectId,
            projectName,
            previousAssigneeId,
            task.AssigneeId,
            currentAssigneeEmail,
            currentAssigneeUserName,
            currentAssigneeDisplayName,
            previousAssigneeUserName);
    }

    public async System.Threading.Tasks.Task<PatchTaskMutationResult?> PatchTaskAsync(
        PatchTaskMutationInput input,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == input.TaskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        if (input.HasAssigneeId && input.AssigneeId is { } assigneeId)
        {
            var exists = await dbContext.Users.AnyAsync(
                u => u.Id == assigneeId && u.OrganizationId == task.OrganizationId,
                cancellationToken);
            if (!exists)
            {
                return null;
            }
        }

        var previousAssigneeId = task.AssigneeId;
        var previousDueDate = task.DueDateUtc;
        var previousStatus = task.Status;

        if (input.HasTitle)
        {
            task.Title = input.Title!;
        }

        if (input.HasDescription)
        {
            task.Description = input.Description;
        }

        if (input.HasStatus && input.Status is { } status)
        {
            task.Status = status;
        }

        if (input.HasPriority && input.Priority is { } priority)
        {
            task.Priority = priority;
        }

        if (input.HasDueDateUtc)
        {
            task.DueDateUtc = input.DueDateUtc;
        }

        if (input.HasAssigneeId)
        {
            task.AssigneeId = input.AssigneeId;
        }

        if (previousAssigneeId != task.AssigneeId || previousDueDate != task.DueDateUtc)
        {
            task.ReminderSent = false;
        }

        task.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PatchTaskMutationResult(
            task.Id,
            task.OrganizationId,
            task.ProjectId,
            previousAssigneeId,
            task.AssigneeId,
            previousDueDate,
            task.DueDateUtc,
            previousStatus,
            task.Status);
    }

    public async System.Threading.Tasks.Task<UpdateTaskMutationResult?> UpdateTaskAsync(
        UpdateTaskMutationInput input,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == input.TaskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        string? previousAssigneeName = null;
        if (task.AssigneeId is { } previousAssigneeId)
        {
            previousAssigneeName = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == previousAssigneeId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        string? currentAssigneeDisplayName = null;
        string? currentAssigneeUserName = null;
        string? currentAssigneeEmail = null;
        if (input.AssigneeId is { } newAssigneeId)
        {
            var assignee = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == newAssigneeId, cancellationToken);
            if (assignee is null || assignee.OrganizationId != task.OrganizationId)
            {
                return null;
            }

            currentAssigneeDisplayName = assignee.DisplayName ?? assignee.UserName ?? string.Empty;
            currentAssigneeUserName = assignee.UserName;
            currentAssigneeEmail = assignee.Email;
        }

        if (input.TagIds is not null &&
            !await TaskTagging.ValidateTagIdsInOrganizationAsync(
                dbContext,
                task.OrganizationId,
                input.TagIds.ToArray(),
                cancellationToken))
        {
            return null;
        }

        if (input.MilestoneId is { } newMilestoneId)
        {
            var milestoneOk = await dbContext.Milestones
                .AsNoTracking()
                .AnyAsync(
                    m => m.Id == newMilestoneId && m.ProjectId == task.ProjectId,
                    cancellationToken);
            if (!milestoneOk)
            {
                return null;
            }
        }

        var previousStatus = task.Status;
        var previousPriority = task.Priority;
        var previousAssignee = task.AssigneeId;
        var previousDueDate = task.DueDateUtc;

        task.Title = input.Title;
        task.Description = input.Description;
        task.Status = input.Status;
        task.Priority = input.Priority;
        task.DueDateUtc = input.DueDateUtc;
        task.AssigneeId = input.AssigneeId;
        task.MilestoneId = input.MilestoneId;
        task.UpdatedAtUtc = DateTime.UtcNow;

        if (previousAssignee != task.AssigneeId || previousDueDate != task.DueDateUtc)
        {
            task.ReminderSent = false;
        }

        if (input.TagIds is not null)
        {
            await TaskTagging.ReplaceTaskTagsAsync(dbContext, task.Id, input.TagIds.ToArray(), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var projectName = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == task.ProjectId && p.OrganizationId == task.OrganizationId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new UpdateTaskMutationResult(
            task.Id,
            task.Title,
            task.OrganizationId,
            task.ProjectId,
            projectName,
            previousAssignee,
            task.AssigneeId,
            previousAssigneeName,
            currentAssigneeDisplayName,
            currentAssigneeUserName,
            currentAssigneeEmail,
            previousStatus,
            task.Status,
            previousPriority,
            task.Priority,
            previousDueDate,
            task.DueDateUtc);
    }

    public async System.Threading.Tasks.Task<DeleteTaskMutationResult?> SoftDeleteTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.IsDeleted = true;
        task.DeletedAt = DateTime.UtcNow;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteTaskMutationResult(task.Id, task.OrganizationId, task.ProjectId, task.AssigneeId, task.Title);
    }

    public async System.Threading.Tasks.Task<DeleteTaskMutationResult?> RestoreTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            return null;
        }

        var task = await dbContext.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.Id == taskId && t.OrganizationId == currentTenant.OrganizationId,
                cancellationToken);
        if (task is null || !task.IsDeleted)
        {
            return null;
        }

        task.IsDeleted = false;
        task.DeletedAt = null;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteTaskMutationResult(task.Id, task.OrganizationId, task.ProjectId, task.AssigneeId, task.Title);
    }

    public async System.Threading.Tasks.Task<DeleteTaskMutationResult?> PermanentDeleteTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            return null;
        }

        var task = await dbContext.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.Id == taskId && t.OrganizationId == currentTenant.OrganizationId,
                cancellationToken);
        if (task is null || !task.IsDeleted)
        {
            return null;
        }

        var result = new DeleteTaskMutationResult(task.Id, task.OrganizationId, task.ProjectId, task.AssigneeId, task.Title);
        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async System.Threading.Tasks.Task<BulkTaskDeleteMutationResult> BulkSoftDeleteTasksAsync(
        IReadOnlyList<Guid> taskIds,
        CancellationToken cancellationToken)
    {
        var ids = taskIds.Distinct().ToArray();
        var tasks = await dbContext.Tasks
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var task in tasks)
        {
            task.IsDeleted = true;
            task.DeletedAt = now;
            task.UpdatedAtUtc = now;
        }

        if (tasks.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var foundIds = tasks.Select(t => t.Id).ToHashSet();
        var notFound = ids.Where(id => !foundIds.Contains(id)).ToArray();
        var mutated = tasks
            .Select(t => new BulkTaskMutationItem(t.Id, t.OrganizationId, t.ProjectId, t.AssigneeId, null))
            .ToList();

        return new BulkTaskDeleteMutationResult(mutated, notFound);
    }

    public async System.Threading.Tasks.Task<BulkTaskUpdateMutationResult> BulkAssignTasksAsync(
        IReadOnlyList<Guid> taskIds,
        Guid? assigneeId,
        CancellationToken cancellationToken)
    {
        var ids = taskIds.Distinct().ToArray();
        var tasks = await dbContext.Tasks
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var foundIds = tasks.Select(t => t.Id).ToHashSet();
        var notFound = ids.Where(id => !foundIds.Contains(id)).ToArray();
        var changedTasks = tasks.Where(t => t.AssigneeId != assigneeId).ToList();

        string? assigneeEmail = null;
        string? assigneeUserName = null;
        string? assigneeDisplayName = null;
        string? workspaceName = null;
        if (assigneeId is { } aid)
        {
            var assignee = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == aid, cancellationToken);
            if (assignee is null || tasks.Any(t => t.OrganizationId != assignee.OrganizationId))
            {
                return new BulkTaskUpdateMutationResult([], notFound, true);
            }

            assigneeEmail = assignee.Email;
            assigneeUserName = assignee.UserName;
            assigneeDisplayName = assignee.DisplayName;
            workspaceName = await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Id == assignee.OrganizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var previousAssignees = changedTasks.ToDictionary(t => t.Id, t => t.AssigneeId);
        var now = DateTime.UtcNow;
        foreach (var task in changedTasks)
        {
            task.AssigneeId = assigneeId;
            task.UpdatedAtUtc = now;
            task.ReminderSent = false;
        }

        if (changedTasks.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var mutated = changedTasks
            .Select(t => new BulkTaskMutationItem(
                t.Id,
                t.OrganizationId,
                t.ProjectId,
                previousAssignees[t.Id],
                t.AssigneeId))
            .ToList();

        return new BulkTaskUpdateMutationResult(
            mutated,
            notFound,
            false,
            assigneeEmail,
            assigneeUserName,
            assigneeDisplayName,
            workspaceName);
    }

    public async System.Threading.Tasks.Task<BulkTaskUpdateMutationResult> BulkUpdateTasksAsync(
        BulkTaskUpdateMutationInput input,
        CancellationToken cancellationToken)
    {
        var ids = input.TaskIds.Distinct().ToArray();
        var tasks = await dbContext.Tasks
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var foundIds = tasks.Select(t => t.Id).ToHashSet();
        var notFound = ids.Where(id => !foundIds.Contains(id)).ToArray();

        if (input.AssigneeId is { } assigneeId)
        {
            var assigneeExists = await dbContext.Users.AnyAsync(
                u => u.Id == assigneeId && tasks.Select(t => t.OrganizationId).Contains(u.OrganizationId),
                cancellationToken);
            if (!assigneeExists)
            {
                return new BulkTaskUpdateMutationResult([], notFound, true);
            }
        }

        var previousAssignees = tasks.ToDictionary(t => t.Id, t => t.AssigneeId);
        var now = DateTime.UtcNow;
        foreach (var task in tasks)
        {
            if (input.Status is { } status)
            {
                task.Status = status;
            }

            if (input.Priority is { } priority)
            {
                task.Priority = priority;
            }

            if (input.HasDueDateUtc)
            {
                task.DueDateUtc = input.DueDateUtc;
            }

            if (input.HasAssigneeId)
            {
                task.AssigneeId = input.AssigneeId;
            }

            task.UpdatedAtUtc = now;
            task.ReminderSent = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var mutated = tasks
            .Select(t => new BulkTaskMutationItem(
                t.Id,
                t.OrganizationId,
                t.ProjectId,
                previousAssignees[t.Id],
                t.AssigneeId))
            .ToList();

        return new BulkTaskUpdateMutationResult(mutated, notFound, false);
    }

    public async System.Threading.Tasks.Task<ChecklistMutationResult?> AddChecklistItemAsync(
        Guid taskId,
        string title,
        int? insertAfterOrder,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var items = await dbContext.ChecklistItems
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);

        var maxOrder = items.Count == 0 ? 0 : items.Max(i => i.Order);
        int newOrder;
        if (insertAfterOrder is null)
        {
            newOrder = maxOrder + 1;
        }
        else
        {
            var after = insertAfterOrder.Value;
            if (maxOrder == 0 || after >= maxOrder)
            {
                newOrder = maxOrder + 1;
            }
            else if (after <= 0)
            {
                foreach (var i in items)
                {
                    i.Order += 1;
                }

                newOrder = 1;
            }
            else
            {
                foreach (var i in items.Where(x => x.Order > after))
                {
                    i.Order += 1;
                }

                newOrder = after + 1;
            }
        }

        var entity = new TaskFlow.Domain.Entities.ChecklistItem
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Title = title.Trim(),
            IsCompleted = false,
            Order = newOrder,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            CompletedAtUtc = null,
        };

        await dbContext.ChecklistItems.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new ChecklistMutationResult(
            task.Id,
            task.OrganizationId,
            task.ProjectId,
            task.AssigneeId,
            new TaskChecklistItemReadModel(entity.Id, entity.Title, entity.IsCompleted, entity.Order, entity.CompletedAtUtc),
            false,
            false,
            true);
    }

    public async System.Threading.Tasks.Task<ChecklistMutationResult?> UpdateChecklistItemAsync(
        Guid taskId,
        Guid itemId,
        string? title,
        bool? isCompleted,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var item = await dbContext.ChecklistItems
            .FirstOrDefaultAsync(c => c.Id == itemId && c.TaskId == taskId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var wasCompleted = item.IsCompleted;
        if (title is not null)
        {
            item.Title = title.Trim();
        }

        if (isCompleted is { } completed)
        {
            item.IsCompleted = completed;
            item.CompletedAtUtc = completed ? timeProvider.GetUtcNow().UtcDateTime : null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var hasIncomplete = await dbContext.ChecklistItems
            .AsNoTracking()
            .AnyAsync(c => c.TaskId == taskId && !c.IsCompleted, cancellationToken);

        return new ChecklistMutationResult(
            task.Id,
            task.OrganizationId,
            task.ProjectId,
            task.AssigneeId,
            new TaskChecklistItemReadModel(item.Id, item.Title, item.IsCompleted, item.Order, item.CompletedAtUtc),
            wasCompleted,
            item.IsCompleted,
            hasIncomplete);
    }

    public async System.Threading.Tasks.Task<ChecklistDeleteMutationResult?> DeleteChecklistItemAsync(
        Guid taskId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var deleted = await dbContext.ChecklistItems
            .Where(c => c.TaskId == taskId && c.Id == itemId)
            .ExecuteDeleteAsync(cancellationToken);

        return new ChecklistDeleteMutationResult(deleted > 0, task.Id, task.OrganizationId, task.ProjectId, task.AssigneeId);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<TaskChecklistItemReadModel>?> ReorderChecklistAsync(
        Guid taskId,
        IReadOnlyList<Guid> orderedIds,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var items = await dbContext.ChecklistItems.Where(c => c.TaskId == taskId).ToListAsync(cancellationToken);
        if (items.Count != orderedIds.Count)
        {
            throw new ValidationException([new ValidationFailure("OrderedIds", "Must include every checklist item exactly once.")]);
        }

        var existingIds = items.Select(i => i.Id).OrderBy(id => id).ToList();
        var requestedSorted = orderedIds.OrderBy(id => id).ToList();
        if (!existingIds.SequenceEqual(requestedSorted))
        {
            throw new ValidationException([new ValidationFailure("OrderedIds", "Must include every checklist item exactly once.")]);
        }

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var id = orderedIds[i];
            var item = items.First(x => x.Id == id);
            item.Order = i + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return orderedIds
            .Select(id => items.First(x => x.Id == id))
            .Select(i => new TaskChecklistItemReadModel(i.Id, i.Title, i.IsCompleted, i.Order, i.CompletedAtUtc))
            .ToList();
    }

    public async System.Threading.Tasks.Task<TaskCommentMutationResult> CreateTaskCommentAsync(
        Guid taskId,
        Guid? authorId,
        string content,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return new TaskCommentMutationResult(StatusCodes.Status404NotFound, taskId, Guid.Empty, Guid.Empty, null, null);
        }

        if (authorId is not { } aid)
        {
            return new TaskCommentMutationResult(StatusCodes.Status401Unauthorized, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        var encoded = HtmlEncoder.Default.Encode(content.Trim());
        if (encoded.Length > 4000)
        {
            return new TaskCommentMutationResult(StatusCodes.Status400BadRequest, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var entity = new TaskFlow.Domain.Entities.Comment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            AuthorId = aid,
            Content = encoded,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsEdited = false,
            IsDeleted = false,
        };
        await dbContext.Comments.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var author = await dbContext.Users.AsNoTracking().FirstAsync(u => u.Id == aid, cancellationToken);
        var comment = new TaskCommentReadModel(
            entity.Id,
            entity.Content,
            entity.IsEdited,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.IsDeleted,
            author.Id,
            author.UserName,
            author.DisplayName);

        return new TaskCommentMutationResult(StatusCodes.Status201Created, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, comment);
    }

    public async System.Threading.Tasks.Task<TaskCommentMutationResult> UpdateTaskCommentAsync(
        Guid taskId,
        Guid commentId,
        Guid? actorUserId,
        string content,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return new TaskCommentMutationResult(StatusCodes.Status404NotFound, taskId, Guid.Empty, Guid.Empty, null, null);
        }

        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId, cancellationToken);
        if (comment is null || comment.IsDeleted)
        {
            return new TaskCommentMutationResult(StatusCodes.Status404NotFound, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        if (actorUserId != comment.AuthorId)
        {
            return new TaskCommentMutationResult(StatusCodes.Status403Forbidden, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        var encoded = HtmlEncoder.Default.Encode(content.Trim());
        if (encoded.Length > 4000)
        {
            return new TaskCommentMutationResult(StatusCodes.Status400BadRequest, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        comment.Content = encoded;
        comment.IsEdited = true;
        comment.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);

        var author = await dbContext.Users.AsNoTracking().FirstAsync(u => u.Id == comment.AuthorId, cancellationToken);
        var model = new TaskCommentReadModel(comment.Id, comment.Content, comment.IsEdited, comment.CreatedAtUtc, comment.UpdatedAtUtc, comment.IsDeleted, author.Id, author.UserName, author.DisplayName);
        return new TaskCommentMutationResult(StatusCodes.Status200OK, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, model);
    }

    public async System.Threading.Tasks.Task<TaskCommentMutationResult> DeleteTaskCommentAsync(
        Guid taskId,
        Guid commentId,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(dbContext, currentTenant, taskId, cancellationToken);
        if (task is null)
        {
            return new TaskCommentMutationResult(StatusCodes.Status404NotFound, taskId, Guid.Empty, Guid.Empty, null, null);
        }

        var comment = await dbContext.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId, cancellationToken);
        if (comment is null)
        {
            return new TaskCommentMutationResult(StatusCodes.Status404NotFound, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        if (actorUserId is not { } uid)
        {
            return new TaskCommentMutationResult(StatusCodes.Status403Forbidden, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid, cancellationToken);
        if (user is null)
        {
            return new TaskCommentMutationResult(StatusCodes.Status403Forbidden, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        var isAuthor = comment.AuthorId == uid;
        var isPrivileged = user.WorkspaceRole is TaskFlow.Domain.Entities.WorkspaceRole.Owner or TaskFlow.Domain.Entities.WorkspaceRole.Admin;
        if (!isAuthor && !isPrivileged)
        {
            return new TaskCommentMutationResult(StatusCodes.Status403Forbidden, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
        }

        if (!comment.IsDeleted)
        {
            comment.IsDeleted = true;
            comment.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new TaskCommentMutationResult(StatusCodes.Status204NoContent, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, null);
    }

    public async System.Threading.Tasks.Task<TaskTagMutationResult> AddTaskTagAsync(
        Guid taskId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return new TaskTagMutationResult(false, false, false, taskId, Guid.Empty, Guid.Empty, null, tagId);
        }

        var tagExists = await dbContext.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Id == tagId && t.OrganizationId == task.OrganizationId, cancellationToken);
        if (!tagExists)
        {
            return new TaskTagMutationResult(true, false, false, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, tagId);
        }

        var already = await dbContext.TaskTags
            .AsNoTracking()
            .AnyAsync(tt => tt.TaskId == taskId && tt.TagId == tagId, cancellationToken);
        if (already)
        {
            return new TaskTagMutationResult(true, true, false, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, tagId);
        }

        await dbContext.TaskTags.AddAsync(new TaskFlow.Domain.Entities.TaskTag { TaskId = taskId, TagId = tagId }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new TaskTagMutationResult(true, true, true, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, tagId);
    }

    public async System.Threading.Tasks.Task<TaskTagMutationResult> RemoveTaskTagAsync(
        Guid taskId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return new TaskTagMutationResult(false, false, false, taskId, Guid.Empty, Guid.Empty, null, tagId);
        }

        var removed = await dbContext.TaskTags
            .Where(tt => tt.TaskId == taskId && tt.TagId == tagId)
            .ExecuteDeleteAsync(cancellationToken);

        return new TaskTagMutationResult(true, true, removed > 0, taskId, task.OrganizationId, task.ProjectId, task.AssigneeId, tagId);
    }

    public async System.Threading.Tasks.Task<TaskDependencyAddMutationResult> AddTaskDependencyAsync(
        Guid taskId,
        Guid blockingTaskId,
        CancellationToken cancellationToken)
    {
        if (taskId == blockingTaskId)
        {
            return new TaskDependencyAddMutationResult("self", taskId, blockingTaskId, null, null, null, null);
        }

        var blocked = await dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        var blocking = await dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == blockingTaskId, cancellationToken);
        if (blocked is null || blocking is null || blocked.OrganizationId != blocking.OrganizationId)
        {
            return new TaskDependencyAddMutationResult("not_found", taskId, blockingTaskId, null, null, null, null);
        }

        var existing = await dbContext.TaskDependencies
            .AnyAsync(d => d.BlockedTaskId == taskId && d.BlockingTaskId == blockingTaskId, cancellationToken);
        if (existing)
        {
            return new TaskDependencyAddMutationResult("duplicate", taskId, blockingTaskId, null, null, blocked.ProjectId, blocking.ProjectId);
        }

        var count = await dbContext.TaskDependencies.CountAsync(d => d.BlockedTaskId == taskId, cancellationToken);
        if (count >= 10)
        {
            return new TaskDependencyAddMutationResult("max", taskId, blockingTaskId, null, null, blocked.ProjectId, blocking.ProjectId);
        }

        if (await WouldCreateCycleAsync(dbContext, taskId, blockingTaskId, cancellationToken))
        {
            return new TaskDependencyAddMutationResult("cycle", taskId, blockingTaskId, null, null, blocked.ProjectId, blocking.ProjectId);
        }

        dbContext.TaskDependencies.Add(new TaskFlow.Domain.Entities.TaskDependency { BlockedTaskId = taskId, BlockingTaskId = blockingTaskId });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaskDependencyAddMutationResult(
            "ok",
            taskId,
            blockingTaskId,
            blocking.Title,
            blocking.Status,
            blocked.ProjectId,
            blocking.ProjectId);
    }

    public async System.Threading.Tasks.Task<TaskDependencyRemoveMutationResult> RemoveTaskDependencyAsync(
        Guid taskId,
        Guid blockingTaskId,
        CancellationToken cancellationToken)
    {
        var blocked = await dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        var blocking = await dbContext.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == blockingTaskId, cancellationToken);
        if (blocked is null || blocking is null)
        {
            return new TaskDependencyRemoveMutationResult(false, null, null);
        }

        var deleted = await dbContext.TaskDependencies
            .Where(d => d.BlockedTaskId == taskId && d.BlockingTaskId == blockingTaskId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
        return new TaskDependencyRemoveMutationResult(deleted, blocked.ProjectId, blocking.ProjectId);
    }

    public async System.Threading.Tasks.Task<CreateTaskMutationResult?> CreateTaskAsync(
        CreateTaskMutationInput input,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TaskFlow.Application.Common.TenantContextMissingException();
        }

        var orgId = currentTenant.OrganizationId;
        var project = await dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == input.ProjectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        TaskFlow.Infrastructure.Identity.ApplicationUser? assignee = null;
        if (input.AssigneeId is { } assigneeId)
        {
            assignee = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);
            if (assignee is null || assignee.OrganizationId != orgId)
            {
                return null;
            }
        }

        if (input.TagIds is { Count: > 0 } tagIdsForValidate &&
            !await TaskFlow.Infrastructure.Features.Tasks.TaskTagging.ValidateTagIdsInOrganizationAsync(
                dbContext,
                orgId,
                tagIdsForValidate.ToArray(),
                cancellationToken))
        {
            return null;
        }

        Guid? milestoneId = null;
        if (input.MilestoneId is { } milestoneValue)
        {
            var milestoneOk = await dbContext.Milestones
                .AsNoTracking()
                .AnyAsync(m => m.Id == milestoneValue && m.ProjectId == input.ProjectId, cancellationToken);
            if (!milestoneOk)
            {
                return null;
            }

            milestoneId = milestoneValue;
        }

        var now = DateTime.UtcNow;
        var task = new TaskFlow.Domain.Entities.Task
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ProjectId = input.ProjectId,
            Title = input.Title,
            Description = input.Description,
            Status = input.Status,
            Priority = input.Priority,
            DueDateUtc = input.DueDateUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            AssigneeId = input.AssigneeId,
            MilestoneId = milestoneId,
            ReminderSent = false,
        };

        await dbContext.Tasks.AddAsync(task, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await TaskFlow.Infrastructure.Features.Tasks.TaskTagging.ReplaceTaskTagsAsync(
            dbContext,
            task.Id,
            input.TagIds?.ToArray(),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateTaskMutationResult(
            task.Id,
            task.Title,
            orgId,
            task.ProjectId,
            project.Name,
            task.AssigneeId,
            assignee?.Email,
            assignee?.UserName,
            assignee?.DisplayName);
    }

    public async System.Threading.Tasks.Task<CreateTaskMutationResult?> CreateTaskFromTemplateAsync(
        CreateTaskFromTemplateMutationInput input,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TaskFlow.Application.Common.TenantContextMissingException();
        }

        var orgId = currentTenant.OrganizationId;
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == input.ProjectId && p.OrganizationId == orgId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var template = await dbContext.TaskTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == input.TemplateId && t.OrganizationId == orgId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        TaskFlow.Infrastructure.Identity.ApplicationUser? assignee = null;
        if (input.OverrideAssigneeId is { } assigneeId)
        {
            assignee = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == assigneeId, cancellationToken);
            if (assignee is null || assignee.OrganizationId != orgId)
            {
                return null;
            }
        }

        var now = DateTime.UtcNow;
        var dueDate = input.OverrideDueDateUtc
            ?? (template.DefaultDueDaysFromNow is { } days ? now.AddDays(days) : null);
        var title = string.IsNullOrWhiteSpace(input.OverrideTitle) ? template.DefaultTitle : input.OverrideTitle!.Trim();
        var description = input.OverrideDescription ?? template.DefaultDescription;
        var priority = input.OverridePriority ?? template.DefaultPriority;

        var task = new TaskFlow.Domain.Entities.Task
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ProjectId = input.ProjectId,
            Title = title,
            Description = description,
            Status = TaskFlow.Domain.Entities.TaskStatus.Todo,
            Priority = priority,
            DueDateUtc = dueDate,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            AssigneeId = input.OverrideAssigneeId,
            ReminderSent = false,
            TemplateId = template.Id,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Tasks.AddAsync(task, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var templateChecklist = await dbContext.TaskTemplateChecklistItems
            .AsNoTracking()
            .Where(i => i.TemplateId == template.Id)
            .OrderBy(i => i.Order)
            .ToListAsync(cancellationToken);
        if (templateChecklist.Count > 0)
        {
            var checklistItems = templateChecklist
                .Select(i => new TaskFlow.Domain.Entities.ChecklistItem
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    Title = i.Title,
                    Order = i.Order,
                    IsCompleted = false,
                    CreatedAtUtc = now,
                    CompletedAtUtc = null,
                })
                .ToList();
            await dbContext.ChecklistItems.AddRangeAsync(checklistItems, cancellationToken);
        }

        var templateTagIds = await dbContext.TaskTemplateTags
            .AsNoTracking()
            .Where(tt => tt.TemplateId == template.Id)
            .Select(tt => tt.TagId)
            .ToArrayAsync(cancellationToken);
        await TaskFlow.Infrastructure.Features.Tasks.TaskTagging.ReplaceTaskTagsAsync(dbContext, task.Id, templateTagIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateTaskMutationResult(
            task.Id,
            task.Title,
            orgId,
            task.ProjectId,
            project.Name,
            task.AssigneeId,
            assignee?.Email,
            assignee?.UserName,
            assignee?.DisplayName);
    }

    private static async System.Threading.Tasks.Task<bool> WouldCreateCycleAsync(
        TaskFlowDbContext db,
        Guid from,
        Guid to,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(to);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == from)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            var deps = await db.TaskDependencies
                .AsNoTracking()
                .Where(d => d.BlockedTaskId == current)
                .Select(d => d.BlockingTaskId)
                .ToListAsync(cancellationToken);
            foreach (var d in deps)
            {
                queue.Enqueue(d);
            }
        }

        return false;
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
