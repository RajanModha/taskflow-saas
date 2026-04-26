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

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class UpdateTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    INotificationService notificationService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger)
    : IRequestHandler<UpdateTaskCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        var previousStatus = task.Status;
        var previousPriority = task.Priority;
        var previousAssigneeId = task.AssigneeId;
        var previousDue = task.DueDateUtc;

        string? previousAssigneeName = null;
        if (previousAssigneeId is { } prevAid)
        {
            var prevUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == prevAid, cancellationToken);
            previousAssigneeName = prevUser?.UserName;
        }

        ApplicationUser? assignee = null;
        if (request.AssigneeId is { } newAssigneeId)
        {
            assignee = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == newAssigneeId, cancellationToken);
            if (assignee is null || assignee.OrganizationId != task.OrganizationId)
            {
                return null;
            }
        }

        if (request.TagIds is not null &&
            !await TaskTagging.ValidateTagIdsInOrganizationAsync(
                dbContext,
                task.OrganizationId,
                request.TagIds,
                cancellationToken))
        {
            return null;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDateUtc = request.DueDateUtc;
        task.AssigneeId = request.AssigneeId;
        task.UpdatedAtUtc = DateTime.UtcNow;

        if (previousAssigneeId != task.AssigneeId || previousDue != task.DueDateUtc)
        {
            task.ReminderSent = false;
        }

        if (request.TagIds is not null)
        {
            await TaskTagging.ReplaceTaskTagsAsync(dbContext, task.Id, request.TagIds, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (currentUser.UserId is { } actorUserId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorUserId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;

            if (previousStatus != request.Status)
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    task.Id,
                    ActivityActions.TaskStatusChanged,
                    actorUserId,
                    actorName,
                    task.OrganizationId,
                    new { from = previousStatus.ToString(), to = request.Status.ToString() },
                    cancellationToken);
            }

            if (previousPriority != request.Priority)
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    task.Id,
                    ActivityActions.TaskPriorityChanged,
                    actorUserId,
                    actorName,
                    task.OrganizationId,
                    new { from = previousPriority.ToString(), to = request.Priority.ToString() },
                    cancellationToken);
            }

            if (previousDue != request.DueDateUtc)
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    task.Id,
                    ActivityActions.TaskDueDateChanged,
                    actorUserId,
                    actorName,
                    task.OrganizationId,
                    new { from = previousDue, to = request.DueDateUtc },
                    cancellationToken);
            }

            if (previousAssigneeId != request.AssigneeId)
            {
                if (request.AssigneeId is null)
                {
                    await activityLogger.LogAsync(
                        ActivityEntityTypes.Task,
                        task.Id,
                        ActivityActions.TaskUnassigned,
                        actorUserId,
                        actorName,
                        task.OrganizationId,
                        new { previousAssigneeId, previousAssigneeName },
                        cancellationToken);
                }
                else if (assignee is not null)
                {
                    var assigneeName = assignee.DisplayName ?? assignee.UserName ?? string.Empty;
                    await activityLogger.LogAsync(
                        ActivityEntityTypes.Task,
                        task.Id,
                        ActivityActions.TaskAssigned,
                        actorUserId,
                        actorName,
                        task.OrganizationId,
                        new { assigneeId = assignee.Id, assigneeName },
                        cancellationToken);
                }
            }
        }

        var assigneeChanged = previousAssigneeId != task.AssigneeId;
        if (assigneeChanged && task.AssigneeId is not null && assignee is not null)
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
            task.AssigneeId);
        boardCacheVersion.BumpProject(task.ProjectId);

        var refreshed = await dbContext.Tasks.AsNoTracking()
            .FirstAsync(t => t.Id == task.Id, cancellationToken);
        var dtoList = await TaskProjection.ToDtosAsync(dbContext, [refreshed], cancellationToken);
        return dtoList[0];
    }
}







