using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Email;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class AssignTaskHandler(
    ITaskWriteRepository taskRepository,
    ITaskReadRepository taskReadRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    INotificationService notificationService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger) : IRequestHandler<AssignTaskCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.AssignTaskAsync(request.TaskId, request.AssigneeId, cancellationToken);
        if (result is null)
        {
            return null;
        }

        if (result.PreviousAssigneeId != result.CurrentAssigneeId && currentUser.UserId is { } actorId)
        {
            if (request.AssigneeId is null)
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    result.TaskId,
                    ActivityActions.TaskUnassigned,
                    actorId,
                    string.Empty,
                    result.OrganizationId,
                    new { previousAssigneeId = result.PreviousAssigneeId, previousAssigneeName = result.PreviousAssigneeUserName },
                    cancellationToken);
            }
            else if (result.CurrentAssigneeId is { } assigneeId)
            {
                var assigneeName = result.CurrentAssigneeDisplayName ?? result.CurrentAssigneeUserName ?? string.Empty;
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    result.TaskId,
                    ActivityActions.TaskAssigned,
                    actorId,
                    string.Empty,
                    result.OrganizationId,
                    new { assigneeId, assigneeName },
                    cancellationToken);
            }
        }

        if (result.CurrentAssigneeId is { } assignedTo &&
            result.CurrentAssigneeEmail is { Length: > 0 } toEmail &&
            result.PreviousAssigneeId != result.CurrentAssigneeId)
        {
            var assigneeName = result.CurrentAssigneeDisplayName ?? result.CurrentAssigneeUserName ?? "there";
            var assignerName = "Someone";
            var baseUrl = emailSettings.Value.FrontendBaseUrl.TrimEnd('/');
            var taskUrl = $"{baseUrl}/tasks/{result.TaskId}";
            await notificationService.CreateAsync(
                assignedTo,
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

        var detached = await taskReadRepository.GetDetachedTaskByIdAsync(result.TaskId, cancellationToken);
        if (detached is null)
        {
            return null;
        }

        var dtoList = await taskReadModelAssembler.ToTaskDtosAsync([detached], cancellationToken);
        return dtoList.Count == 0 ? null : dtoList[0];
    }
}







