using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks;

internal static class TaskProjection
{
    internal static decimal ChecklistProgress(int total, int completed)
    {
        if (total == 0)
        {
            return 0;
        }

        return Math.Round((decimal)completed * 100m / total, 1, MidpointRounding.AwayFromZero);
    }

    public static TaskDto ToDto(
        DomainTask task,
        TaskAssigneeDto? assignee,
        int commentCount,
        IReadOnlyList<TagDto> tags,
        int checklistTotal,
        int checklistCompleted,
        decimal checklistProgress) =>
        new(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDateUtc,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            assignee,
            commentCount,
            tags,
            checklistTotal,
            checklistCompleted,
            checklistProgress,
            task.IsDeleted,
            task.DeletedAt);

    public static async Task<List<TaskDto>> ToDtosAsync(
        TaskFlowDbContext dbContext,
        IReadOnlyList<DomainTask> tasks,
        CancellationToken cancellationToken)
    {
        if (tasks.Count == 0)
        {
            return [];
        }

        var assigneeIds = tasks.Where(t => t.AssigneeId.HasValue).Select(t => t.AssigneeId!.Value).Distinct().ToList();
        Dictionary<Guid, ApplicationUser> users = new();
        if (assigneeIds.Count > 0)
        {
            users = await dbContext.Users
                .AsNoTracking()
                .Where(u => assigneeIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);
        }

        var taskIds = tasks.Select(t => t.Id).ToList();
        var commentCounts = await dbContext.Comments
            .AsNoTracking()
            .Where(c => taskIds.Contains(c.TaskId) && !c.IsDeleted)
            .GroupBy(c => c.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TaskId, x => x.Count, cancellationToken);

        var checklistStats = await dbContext.ChecklistItems
            .AsNoTracking()
            .Where(ci => taskIds.Contains(ci.TaskId))
            .GroupBy(ci => ci.TaskId)
            .Select(g => new { TaskId = g.Key, Total = g.Count(), Completed = g.Count(x => x.IsCompleted) })
            .ToDictionaryAsync(x => x.TaskId, x => (x.Total, x.Completed), cancellationToken);

        var tagRows = await dbContext.TaskTags
            .AsNoTracking()
            .Where(tt => taskIds.Contains(tt.TaskId))
            .Join(
                dbContext.Tags.AsNoTracking(),
                tt => tt.TagId,
                tg => tg.Id,
                (tt, tg) => new { tt.TaskId, Tag = tg })
            .ToListAsync(cancellationToken);

        var tagsByTask = tagRows
            .GroupBy(x => x.TaskId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<TagDto>)g
                    .Select(x => new TagDto(x.Tag.Id, x.Tag.Name, x.Tag.Color))
                    .OrderBy(t => t.Name)
                    .ToList());

        return tasks.Select(t =>
            {
                TaskAssigneeDto? assignee = null;
                if (t.AssigneeId is { } aid && users.TryGetValue(aid, out var u))
                {
                    assignee = new TaskAssigneeDto(u.Id, u.UserName ?? string.Empty, u.DisplayName);
                }

                commentCounts.TryGetValue(t.Id, out var count);
                tagsByTask.TryGetValue(t.Id, out var tags);
                checklistStats.TryGetValue(t.Id, out var cl);
                var total = cl.Total;
                var completed = cl.Completed;
                return ToDto(t, assignee, count, tags ?? [], total, completed, ChecklistProgress(total, completed));
            })
            .ToList();
    }
}
