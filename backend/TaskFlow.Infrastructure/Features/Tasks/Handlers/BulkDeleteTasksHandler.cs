using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class BulkDeleteTasksHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<BulkDeleteTasksCommand, BulkTaskDeleteResultDto>
{
    public async Task<BulkTaskDeleteResultDto> Handle(BulkDeleteTasksCommand request, CancellationToken cancellationToken)
    {
        var ids = request.TaskIds.Distinct().ToArray();
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

        foreach (var task in tasks)
        {
            DashboardCacheInvalidation.InvalidateAfterTaskMutation(
                cache,
                task.OrganizationId,
                currentUser.UserId,
                task.AssigneeId,
                null);
            boardCacheVersion.BumpProject(task.ProjectId);
        }

        var foundIds = tasks.Select(t => t.Id).ToHashSet();
        var notFound = ids.Where(id => !foundIds.Contains(id)).ToArray();
        return new BulkTaskDeleteResultDto(tasks.Count, notFound);
    }
}
