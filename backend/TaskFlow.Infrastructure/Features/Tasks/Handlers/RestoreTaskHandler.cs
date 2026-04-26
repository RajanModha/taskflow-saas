using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Tenancy;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class RestoreTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<RestoreTaskCommand, TaskDto?>
{
    public async Task<TaskDto?> Handle(RestoreTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.Id == request.TaskId &&
                     currentTenant.IsSet &&
                     t.OrganizationId == currentTenant.OrganizationId,
                cancellationToken);
        if (task is null || !task.IsDeleted)
        {
            return null;
        }

        task.IsDeleted = false;
        task.DeletedAt = null;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(cache, task.OrganizationId, currentUser.UserId, null, task.AssigneeId);
        boardCacheVersion.BumpProject(task.ProjectId);

        var dto = await TaskProjection.ToDtosAsync(dbContext, [task], cancellationToken);
        return dto[0];
    }
}
