using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using DomainTask = TaskFlow.Domain.Entities.Task;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Projects;

internal static class BoardMapper
{
    internal static string Fingerprint(Guid? assigneeId, Guid? tagId, string? q)
    {
        var qn = q?.Trim() ?? string.Empty;
        if (qn.Length > 512)
        {
            qn = qn[..512];
        }

        return $"{assigneeId?.ToString("N") ?? "-"}:{tagId?.ToString("N") ?? "-"}:{qn}";
    }

    /// <summary>Maps backlog tasks into the Todo column.</summary>
    internal static DomainTaskStatus ColumnFor(DomainTaskStatus status) =>
        status == DomainTaskStatus.Backlog ? DomainTaskStatus.Todo : status;

    internal static BoardTaskDto ToBoardTask(
        DomainTask task,
        IReadOnlyDictionary<Guid, ApplicationUser> assigneesById,
        IReadOnlyDictionary<Guid, int> commentCountByTaskId,
        DateTime nowUtc)
    {
        TaskAssigneeDto? assignee = null;
        if (task.AssigneeId is { } aid && assigneesById.TryGetValue(aid, out var u))
        {
            assignee = new TaskAssigneeDto(u.Id, u.UserName ?? string.Empty, u.DisplayName);
        }

        commentCountByTaskId.TryGetValue(task.Id, out var commentCount);

        var tags = task.TaskTags
            .OrderBy(tt => tt.Tag.Name)
            .Select(tt => new TagDto(tt.Tag.Id, tt.Tag.Name, tt.Tag.Color))
            .ToList();

        var isOverdue = task.DueDateUtc is { } due
            && due < nowUtc
            && task.Status != DomainTaskStatus.Done
            && task.Status != DomainTaskStatus.Cancelled;

        return new BoardTaskDto(
            task.Id,
            task.Title,
            task.Priority,
            task.DueDateUtc,
            isOverdue,
            assignee,
            tags,
            commentCount,
            task.CreatedAtUtc);
    }

    internal static IReadOnlyList<BoardTaskDto> SortBoardTasks(IEnumerable<BoardTaskDto> tasks) =>
        tasks
            .OrderByDescending(t => (int)t.Priority)
            .ThenBy(t => t.DueDateUtc ?? DateTime.MaxValue)
            .ThenBy(t => t.Id)
            .ToList();
}
