using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class DeleteTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger) : IRequestHandler<DeleteTaskCommand, bool>
{
    public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return false;
        }

        var orgId = task.OrganizationId;
        var taskId = task.Id;
        var title = task.Title;
        var projectId = task.ProjectId;
        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                taskId,
                ActivityActions.TaskDeleted,
                actorId,
                actorName,
                orgId,
                new { title },
                cancellationToken);
        }

        cache.Remove(DashboardCacheKeys.DashboardStats(orgId));
        boardCacheVersion.BumpProject(projectId);
        return true;
    }
}

