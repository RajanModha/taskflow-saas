using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

/// <summary>
/// Board load: split query for tasks + task tags + tags; batched comment counts (IsDeleted = false); batched assignee users.
/// Domain <see cref="TaskFlow.Domain.Entities.Task"/> has no Assignee navigation (Identity lives in Infrastructure), so assignees are not EF-Included.
/// </summary>
public sealed class GetProjectBoardHandler(
    IProjectReadRepository projectReadRepository,
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
        var boardData = await projectReadRepository.GetProjectBoardDataAsync(
            request.ProjectId,
            request.AssigneeId,
            request.TagId,
            request.Q,
            cancellationToken);
        if (boardData is null)
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

        var tasks = boardData.Value.Tasks;

        IReadOnlyDictionary<DomainTaskStatus, IReadOnlyList<BoardTaskDto>> byColumn;
        if (tasks.Count == 0)
        {
            byColumn = new Dictionary<DomainTaskStatus, IReadOnlyList<BoardTaskDto>>();
        }
        else
        {
            byColumn = tasks
                .GroupBy(t => BoardMapper.ColumnFor(t.Status))
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<BoardTaskDto>)BoardMapper.SortBoardTasks(
                        g.Select(task => ToBoardTask(task, nowUtc))));
        }

        var response = BuildResponse(boardData.Value.Project.Id, boardData.Value.Project.Name, byColumn);
        TrySetCache(request.ProjectId, filtersFingerprint, version, response);
        return response;
    }

    private static BoardTaskDto ToBoardTask(ProjectBoardTaskReadModel task, DateTime nowUtc)
    {
        var assignee = task.AssigneeId is { } aid
            ? new TaskAssigneeDto(aid, task.AssigneeUserName ?? string.Empty, task.AssigneeDisplayName)
            : null;
        var tags = task.Tags.Select(t => new TagDto(t.Id, t.Name, t.Color)).ToList();
        return new BoardTaskDto(
            task.TaskId,
            task.Title,
            task.Priority,
            task.DueDateUtc,
            task.DueDateUtc.HasValue && task.DueDateUtc.Value < nowUtc,
            assignee,
            tags,
            task.CommentCount,
            task.CreatedAtUtc);
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
