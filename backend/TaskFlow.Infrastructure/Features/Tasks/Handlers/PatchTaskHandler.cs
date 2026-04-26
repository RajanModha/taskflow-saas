using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class PatchTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<PatchTaskCommand, TaskDto?>
{
    public async Task<TaskDto?> Handle(PatchTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        if (request.HasAssigneeId && request.AssigneeId is { } assigneeId)
        {
            var exists = await dbContext.Users.AnyAsync(
                u => u.Id == assigneeId && u.OrganizationId == task.OrganizationId,
                cancellationToken);
            if (!exists)
            {
                return null;
            }
        }

        var previousAssigneeId = task.AssigneeId;
        var previousDueDate = task.DueDateUtc;
        if (request.HasTitle)
        {
            task.Title = request.Title!;
        }

        if (request.HasDescription)
        {
            task.Description = request.Description;
        }

        if (request.HasStatus && request.Status is { } status)
        {
            task.Status = status;
        }

        if (request.HasPriority && request.Priority is { } priority)
        {
            task.Priority = priority;
        }

        if (request.HasDueDateUtc)
        {
            task.DueDateUtc = request.DueDateUtc;
        }

        if (request.HasAssigneeId)
        {
            task.AssigneeId = request.AssigneeId;
        }

        if (previousAssigneeId != task.AssigneeId || previousDueDate != task.DueDateUtc)
        {
            task.ReminderSent = false;
        }

        task.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            task.OrganizationId,
            currentUser.UserId,
            previousAssigneeId,
            task.AssigneeId);
        boardCacheVersion.BumpProject(task.ProjectId);

        var refreshed = await dbContext.Tasks.AsNoTracking().FirstAsync(t => t.Id == task.Id, cancellationToken);
        var dtoList = await TaskProjection.ToDtosAsync(dbContext, [refreshed], cancellationToken);
        return dtoList[0];
    }
}
