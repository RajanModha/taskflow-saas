using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class AssignTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    INotificationService notificationService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger) : IRequestHandler<AssignTaskCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        var previousAssigneeId = task.AssigneeId;

        ApplicationUser? assignee = null;
        if (request.AssigneeId is { } newId)
        {
            assignee = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == newId, cancellationToken);
            if (assignee is null || assignee.OrganizationId != task.OrganizationId)
            {
                return null;
            }
        }

        task.AssigneeId = request.AssigneeId;
        task.UpdatedAtUtc = DateTime.UtcNow;

        if (previousAssigneeId != task.AssigneeId)
        {
            task.ReminderSent = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousAssigneeId != request.AssigneeId && currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            if (request.AssigneeId is null)
            {
                string? previousName = null;
                if (previousAssigneeId is { } pId)
                {
                    var pu = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == pId, cancellationToken);
                    previousName = pu?.UserName;
                }

                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    task.Id,
                    ActivityActions.TaskUnassigned,
                    actorId,
                    actorName,
                    task.OrganizationId,
                    new { previousAssigneeId, previousAssigneeName = previousName },
                    cancellationToken);
            }
            else if (assignee is not null)
            {
                var assigneeName = assignee.DisplayName ?? assignee.UserName ?? string.Empty;
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    task.Id,
                    ActivityActions.TaskAssigned,
                    actorId,
                    actorName,
                    task.OrganizationId,
                    new { assigneeId = assignee.Id, assigneeName },
                    cancellationToken);
            }
        }

        if (request.AssigneeId is not null && assignee is not null && previousAssigneeId != request.AssigneeId)
        {
            var project = await dbContext.Projects
                .AsNoTracking()
                .FirstAsync(p => p.Id == task.ProjectId && p.OrganizationId == task.OrganizationId, cancellationToken);

            await TaskAssignmentNotifier.NotifyAssigneeAsync(
                dbContext,
                notificationService,
                emailSettings,
                currentUser.UserId,
                task,
                project.Name,
                assignee,
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            task.OrganizationId,
            currentUser.UserId,
            previousAssigneeId,
            request.AssigneeId);
        boardCacheVersion.BumpProject(task.ProjectId);

        var refreshed = await dbContext.Tasks.AsNoTracking()
            .FirstAsync(t => t.Id == task.Id, cancellationToken);
        var dtoList = await TaskProjection.ToDtosAsync(dbContext, [refreshed], cancellationToken);
        return dtoList[0];
    }
}







