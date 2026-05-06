using MediatR;
using Task = System.Threading.Tasks.Task;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class PatchTaskHandler(
    ITaskRepository taskRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IWebhookDispatcher webhookDispatcher)
    : IRequestHandler<PatchTaskCommand, TaskDto?>
{
    public async Task<TaskDto?> Handle(PatchTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.PatchTaskAsync(
            new PatchTaskMutationInput(
                request.TaskId,
                request.Title,
                request.HasTitle,
                request.Description,
                request.HasDescription,
                request.Status,
                request.HasStatus,
                request.Priority,
                request.HasPriority,
                request.DueDateUtc,
                request.HasDueDateUtc,
                request.AssigneeId,
                request.HasAssigneeId),
            cancellationToken);
        if (result is null)
        {
            return null;
        }

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            result.OrganizationId,
            currentUser.UserId,
            result.PreviousAssigneeId,
            result.CurrentAssigneeId);
        boardCacheVersion.BumpProject(result.ProjectId);

        if (request.HasStatus && request.Status is DomainTaskStatus newStatus && result.PreviousStatus != newStatus)
        {
            await webhookDispatcher.DispatchOrganizationEventAsync(
                result.OrganizationId,
                WebhookEventTypes.TaskStatusChanged,
                new
                {
                    taskId = result.TaskId,
                    projectId = result.ProjectId,
                    fromStatus = result.PreviousStatus.ToString(),
                    toStatus = newStatus.ToString(),
                },
                cancellationToken);
        }

        if (request.HasAssigneeId &&
            result.PreviousAssigneeId != result.CurrentAssigneeId &&
            result.CurrentAssigneeId is { } newAssigneeId)
        {
            await webhookDispatcher.DispatchOrganizationEventAsync(
                result.OrganizationId,
                WebhookEventTypes.TaskAssigned,
                new { taskId = result.TaskId, projectId = result.ProjectId, assigneeId = newAssigneeId },
                cancellationToken);
        }

        var detached = await taskRepository.GetDetachedTaskByIdAsync(result.TaskId, cancellationToken);
        if (detached is null)
        {
            return null;
        }

        var dtoList = await taskReadModelAssembler.ToTaskDtosAsync([detached], cancellationToken);
        return dtoList.Count == 0 ? null : dtoList[0];
    }
}
