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

public sealed class CreateTaskHandler(
    ITaskRepository taskRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    INotificationService notificationService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    IWebhookDispatcher webhookDispatcher) : IRequestHandler<CreateTaskCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var created = await taskRepository.CreateTaskAsync(
            new CreateTaskMutationInput(
                request.ProjectId,
                request.Title,
                request.Description,
                request.Status,
                request.Priority,
                request.DueDateUtc,
                request.AssigneeId,
                request.TagIds,
                request.MilestoneId),
            cancellationToken);
        if (created is null)
        {
            return null;
        }

        if (created.AssigneeId is { } assigneeId && created.AssigneeEmail is { Length: > 0 } toEmail)
        {
            var assigneeName = created.AssigneeDisplayName ?? created.AssigneeUserName ?? "there";
            var assignerName = "Someone";
            var baseUrl = emailSettings.Value.FrontendBaseUrl.TrimEnd('/');
            var taskUrl = $"{baseUrl}/tasks/{created.TaskId}";
            await notificationService.CreateAsync(
                assigneeId,
                "task.assigned",
                "Task assigned",
                $"{assignerName} assigned you '{created.TaskTitle}'",
                entityType: "Task",
                entityId: created.TaskId,
                sendEmail: true,
                toEmail: toEmail,
                emailSubject: $"You've been assigned: {created.TaskTitle}",
                emailHtml: EmailTemplates.TaskAssigned(
                    assigneeName,
                    created.TaskTitle,
                    created.ProjectName,
                    assignerName,
                    taskUrl),
                ct: cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, created.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, created.AssigneeId);
        boardCacheVersion.BumpProject(created.ProjectId);

        if (currentUser.UserId is { } creatorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                created.TaskId,
                ActivityActions.TaskCreated,
                creatorId,
                string.Empty,
                created.OrganizationId,
                new { projectId = created.ProjectId, title = created.TaskTitle },
                cancellationToken);
        }

        var detached = await taskRepository.GetDetachedTaskByIdAsync(created.TaskId, cancellationToken);
        if (detached is null)
        {
            return null;
        }
        var dtoList = await taskReadModelAssembler.ToTaskDtosAsync([detached], cancellationToken);

        await webhookDispatcher.DispatchOrganizationEventAsync(
            created.OrganizationId,
            WebhookEventTypes.TaskCreated,
            new { taskId = created.TaskId, projectId = created.ProjectId, title = created.TaskTitle },
            cancellationToken);

        return dtoList.Count == 0 ? null : dtoList[0];
    }
}

