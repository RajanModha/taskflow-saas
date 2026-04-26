using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

/// <summary>
/// Board load: split query for tasks + task tags + tags; batched comment counts (IsDeleted = false); batched assignee users.
/// Domain <see cref="TaskFlow.Domain.Entities.Task"/> has no Assignee navigation (Identity lives in Infrastructure), so assignees are not EF-Included.
/// </summary>
public sealed class GetProjectBoardHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    TimeProvider timeProvider)
    : IRequestHandler<GetProjectBoardQuery, ProjectBoardResponse?>
{
    private static readonly (DomainTaskStatus Status, string DisplayName, string Color)[] ColumnDefinitions =
    [
        (DomainTaskStatus.Todo, "To Do", "#6366f1"),
        (DomainTaskStatus.InProgress, "In Progress", "#f59e0b"),
        (DomainTaskStatus.Done, "Done", "#22c55e"),
        (DomainTaskStatus.Cancelled, "Cancelled", "#71717a"),
    ];

    public async Task<ProjectBoardResponse?> Handle(GetProjectBoardQuery request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return null;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var version = boardCacheVersion.GetSnapshot(request.ProjectId);

        var filtersFingerprint = BoardMapper.Fingerprint(request.AssigneeId, request.TagId, request.Q);
        if (currentUser.UserId is { } userId)
        {
            var cacheKey = BoardCacheKey(request.ProjectId, userId);
            if (cache.TryGetValue(cacheKey, out object? cachedObj) &&
                cachedObj is BoardCacheEnvelope envelope &&
                envelope.Version == version &&
                string.Equals(envelope.FiltersFingerprint, filtersFingerprint, StringComparison.Ordinal))
            {
                return envelope.Payload;
            }
        }

        var query = dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == request.ProjectId);

        if (request.AssigneeId.HasValue)
        {
            query = query.Where(t => t.AssigneeId == request.AssigneeId.Value);
        }

        if (request.TagId.HasValue)
        {
            var tagId = request.TagId.Value;
            query = query.Where(t => dbContext.TaskTags.Any(tt => tt.TaskId == t.Id && tt.TagId == tagId));
        }

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var qNorm = request.Q.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(qNorm));
        }

        var tasks = await query
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<DomainTaskStatus, IReadOnlyList<BoardTaskDto>> byColumn;
        if (tasks.Count == 0)
        {
            byColumn = new Dictionary<DomainTaskStatus, IReadOnlyList<BoardTaskDto>>();
        }
        else
        {
            var taskIds = tasks.Select(t => t.Id).ToList();

            var commentCounts = await dbContext.Comments
                .AsNoTracking()
                .Where(c => taskIds.Contains(c.TaskId) && !c.IsDeleted)
                .GroupBy(c => c.TaskId)
                .Select(g => new { TaskId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TaskId, x => x.Count, cancellationToken);

            var assigneeIds = tasks.Where(t => t.AssigneeId.HasValue).Select(t => t.AssigneeId!.Value).Distinct().ToList();
            Dictionary<Guid, ApplicationUser> assignees = new();
            if (assigneeIds.Count > 0)
            {
                assignees = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => assigneeIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, cancellationToken);
            }

            byColumn = tasks
                .GroupBy(t => BoardMapper.ColumnFor(t.Status))
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<BoardTaskDto>)BoardMapper.SortBoardTasks(
                        g.Select(task => BoardMapper.ToBoardTask(task, assignees, commentCounts, nowUtc))));
        }

        var response = BuildResponse(project.Id, project.Name, byColumn);
        TrySetCache(request.ProjectId, filtersFingerprint, version, response);
        return response;
    }

    private void TrySetCache(
        Guid projectId,
        string filtersFingerprint,
        long version,
        ProjectBoardResponse payload)
    {
        if (currentUser.UserId is not { } userId)
        {
            return;
        }

        var cacheKey = BoardCacheKey(projectId, userId);
        cache.Set(
            cacheKey,
            new BoardCacheEnvelope(version, filtersFingerprint, payload),
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) });
    }

    /// <summary>Spec: board:{projectId}:{currentUserId}. Filters are matched via <see cref="BoardCacheEnvelope"/>.</summary>
    private static string BoardCacheKey(Guid projectId, Guid userId) => $"board:{projectId}:{userId}";

    private static ProjectBoardResponse BuildResponse(
        Guid projectId,
        string projectName,
        IReadOnlyDictionary<DomainTaskStatus, IReadOnlyList<BoardTaskDto>> byColumn)
    {
        var columns = new List<BoardColumnDto>(4);
        foreach (var (status, displayName, color) in ColumnDefinitions)
        {
            byColumn.TryGetValue(status, out var list);
            list ??= [];
            columns.Add(
                new BoardColumnDto(
                    status.ToString(),
                    (int)status,
                    displayName,
                    color,
                    list.Count,
                    list));
        }

        return new ProjectBoardResponse(projectId, projectName, columns);
    }

    private sealed record BoardCacheEnvelope(long Version, string FiltersFingerprint, ProjectBoardResponse Payload);
}
