using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Workspaces;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class DeleteTaskHandler(
    ITaskRepository taskRepository,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    IWebhookDispatcher webhookDispatcher) : IRequestHandler<DeleteTaskCommand, bool>
{
    public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var deleted = await taskRepository.SoftDeleteTaskAsync(request.TaskId, cancellationToken);
        if (deleted is null)
        {
            return false;
        }

        if (currentUser.UserId is { } actorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                deleted.TaskId,
                ActivityActions.TaskDeleted,
                actorId,
                string.Empty,
                deleted.OrganizationId,
                new { title = deleted.Title },
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            deleted.OrganizationId,
            currentUser.UserId,
            deleted.AssigneeId,
            null);
        boardCacheVersion.BumpProject(deleted.ProjectId);

        await webhookDispatcher.DispatchOrganizationEventAsync(
            deleted.OrganizationId,
            WebhookEventTypes.TaskDeleted,
            new { taskId = deleted.TaskId, projectId = deleted.ProjectId, title = deleted.Title },
            cancellationToken);

        return true;
    }
}

