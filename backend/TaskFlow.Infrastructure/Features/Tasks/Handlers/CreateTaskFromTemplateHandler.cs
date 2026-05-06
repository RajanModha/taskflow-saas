using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class CreateTaskFromTemplateHandler(
    ITaskRepository taskRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUser currentUser,
    IOptions<EmailSettings> emailSettings,
    INotificationService notificationService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    IWebhookDispatcher webhookDispatcher)
    : IRequestHandler<CreateTaskFromTemplateCommand, TaskDto?>
{
    public async Task<TaskDto?> Handle(CreateTaskFromTemplateCommand request, CancellationToken cancellationToken)
    {
        var created = await taskRepository.CreateTaskFromTemplateAsync(
            new CreateTaskFromTemplateMutationInput(
                request.TemplateId,
                request.ProjectId,
                request.Overrides?.Title,
                request.Overrides?.Description,
                request.Overrides?.Priority,
                request.Overrides?.DueDateUtc,
                request.Overrides?.AssigneeId),
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

        if (currentUser.UserId is { } actorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                created.TaskId,
                ActivityActions.TaskCreatedFromTemplate,
                actorId,
                string.Empty,
                created.OrganizationId,
                new { projectId = created.ProjectId, title = created.TaskTitle },
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, created.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, created.AssigneeId);
        boardCacheVersion.BumpProject(created.ProjectId);

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
