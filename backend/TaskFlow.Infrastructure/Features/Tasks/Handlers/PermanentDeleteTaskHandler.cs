using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Tenancy;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class PermanentDeleteTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<PermanentDeleteTaskCommand, bool>
{
    public async Task<bool> Handle(PermanentDeleteTaskCommand request, CancellationToken cancellationToken)
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
            return false;
        }

        var orgId = task.OrganizationId;
        var assigneeId = task.AssigneeId;
        var projectId = task.ProjectId;

        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(cache, orgId, currentUser.UserId, assigneeId, null);
        boardCacheVersion.BumpProject(projectId);
        return true;
    }
}
