using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Workspaces;
using TaskFlow.Application.Notifications;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Email;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class UpdateTaskHandler(
    ITaskWriteRepository taskRepository,
    ITaskReadRepository taskReadRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    INotificationService notificationService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    IWebhookDispatcher webhookDispatcher)
    : IRequestHandler<UpdateTaskCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.UpdateTaskAsync(
            new UpdateTaskMutationInput(
                request.TaskId,
                request.Title,
                request.Description,
                request.Status,
                request.Priority,
                request.DueDateUtc,
                request.AssigneeId,
                request.TagIds,
                request.MilestoneId),
            cancellationToken);
        if (result is null)
        {
            return null;
        }

        if (currentUser.UserId is { } actorUserId)
        {
            if (result.PreviousStatus != result.CurrentStatus)
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    result.TaskId,
                    ActivityActions.TaskStatusChanged,
                    actorUserId,
                    string.Empty,
                    result.OrganizationId,
                    new { from = result.PreviousStatus.ToString(), to = result.CurrentStatus.ToString() },
                    cancellationToken);
            }

            if (result.PreviousPriority != result.CurrentPriority)
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    result.TaskId,
                    ActivityActions.TaskPriorityChanged,
                    actorUserId,
                    string.Empty,
                    result.OrganizationId,
                    new { from = result.PreviousPriority.ToString(), to = result.CurrentPriority.ToString() },
                    cancellationToken);
            }

            if (result.PreviousDueDateUtc != result.CurrentDueDateUtc)
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    result.TaskId,
                    ActivityActions.TaskDueDateChanged,
                    actorUserId,
                    string.Empty,
                    result.OrganizationId,
                    new { from = result.PreviousDueDateUtc, to = result.CurrentDueDateUtc },
                    cancellationToken);
            }

            if (result.PreviousAssigneeId != result.CurrentAssigneeId)
            {
                if (result.CurrentAssigneeId is null)
                {
                    await activityLogger.LogAsync(
                        ActivityEntityTypes.Task,
                        result.TaskId,
                        ActivityActions.TaskUnassigned,
                        actorUserId,
                        string.Empty,
                        result.OrganizationId,
                        new { previousAssigneeId = result.PreviousAssigneeId, previousAssigneeName = result.PreviousAssigneeUserName },
                        cancellationToken);
                }
                else if (result.CurrentAssigneeId is { } activityAssigneeId)
                {
                    var assigneeName = result.CurrentAssigneeDisplayName ?? result.CurrentAssigneeUserName ?? string.Empty;
                    await activityLogger.LogAsync(
                        ActivityEntityTypes.Task,
                        result.TaskId,
                        ActivityActions.TaskAssigned,
                        actorUserId,
                        string.Empty,
                        result.OrganizationId,
                        new { assigneeId = activityAssigneeId, assigneeName },
                        cancellationToken);
                }
            }
        }

        var assigneeChanged = result.PreviousAssigneeId != result.CurrentAssigneeId;
        if (assigneeChanged &&
            result.CurrentAssigneeId is { } assigneeId &&
            result.CurrentAssigneeEmail is { Length: > 0 } toEmail)
        {
            var assigneeName = result.CurrentAssigneeDisplayName ?? result.CurrentAssigneeUserName ?? "there";
            var assignerName = "Someone";
            var baseUrl = emailSettings.Value.FrontendBaseUrl.TrimEnd('/');
            var taskUrl = $"{baseUrl}/tasks/{result.TaskId}";
            await notificationService.CreateAsync(
                assigneeId,
                "task.assigned",
                "Task assigned",
                $"{assignerName} assigned you '{result.TaskTitle}'",
                entityType: "Task",
                entityId: result.TaskId,
                sendEmail: true,
                toEmail: toEmail,
                emailSubject: $"You've been assigned: {result.TaskTitle}",
                emailHtml: EmailTemplates.TaskAssigned(
                    assigneeName,
                    result.TaskTitle,
                    result.ProjectName,
                    assignerName,
                    taskUrl),
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            result.OrganizationId,
            currentUser.UserId,
            result.PreviousAssigneeId,
            result.CurrentAssigneeId);
        boardCacheVersion.BumpProject(result.ProjectId);

        if (result.PreviousStatus != result.CurrentStatus)
        {
            await webhookDispatcher.DispatchOrganizationEventAsync(
                result.OrganizationId,
                WebhookEventTypes.TaskStatusChanged,
                new
                {
                    taskId = result.TaskId,
                    projectId = result.ProjectId,
                    fromStatus = result.PreviousStatus.ToString(),
                    toStatus = result.CurrentStatus.ToString(),
                },
                cancellationToken);
        }

        if (result.PreviousAssigneeId != result.CurrentAssigneeId && result.CurrentAssigneeId is { } webhookAssigneeId)
        {
            await webhookDispatcher.DispatchOrganizationEventAsync(
                result.OrganizationId,
                WebhookEventTypes.TaskAssigned,
                new { taskId = result.TaskId, projectId = result.ProjectId, assigneeId = webhookAssigneeId },
                cancellationToken);
        }

        var detached = await taskReadRepository.GetDetachedTaskByIdAsync(result.TaskId, cancellationToken);
        if (detached is null)
        {
            return null;
        }

        var dtoList = await taskReadModelAssembler.ToTaskDtosAsync([detached], cancellationToken);
        return dtoList.Count == 0 ? null : dtoList[0];
    }
}







