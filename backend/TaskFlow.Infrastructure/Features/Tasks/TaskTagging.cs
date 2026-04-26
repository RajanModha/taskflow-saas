using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks;

internal static class TaskTagging
{
    public static async System.Threading.Tasks.Task<bool> ValidateTagIdsInOrganizationAsync(
        TaskFlowDbContext dbContext,
        Guid organizationId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return true;
        }

        var distinct = tagIds.Distinct().ToList();
        var count = await dbContext.Tags
            .AsNoTracking()
            .CountAsync(t => distinct.Contains(t.Id) && t.OrganizationId == organizationId, cancellationToken);
        return count == distinct.Count;
    }

    public static async System.Threading.Tasks.Task ReplaceTaskTagsAsync(
        TaskFlowDbContext dbContext,
        Guid taskId,
        IReadOnlyList<Guid>? tagIds,
        CancellationToken cancellationToken)
    {
        await dbContext.TaskTags
            .Where(tt => tt.TaskId == taskId)
            .ExecuteDeleteAsync(cancellationToken);

        if (tagIds is null || tagIds.Count == 0)
        {
            return;
        }

        var distinct = tagIds.Distinct().ToList();
        var links = distinct.Select(
                tagId => new TaskTag
                {
                    TaskId = taskId,
                    TagId = tagId,
                })
            .ToList();

        await dbContext.TaskTags.AddRangeAsync(links, cancellationToken);
    }
}
