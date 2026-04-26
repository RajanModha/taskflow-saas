using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class TaskRepository(TaskFlowDbContext dbContext, ICurrentUser currentUser) : ITaskRepository
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
