using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class UpdateChecklistItemHandler(
    ITaskChecklistRepository taskRepository,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    INotificationService notificationService,
    IMemoryCache cache)
    : IRequestHandler<UpdateChecklistItemCommand, ChecklistItemDto?>
{
    public async Task<ChecklistItemDto?> Handle(UpdateChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.UpdateChecklistItemAsync(
            request.TaskId,
            request.ItemId,
            request.Title,
            request.IsCompleted,
            cancellationToken);
        if (result is null)
        {
            return null;
        }
        if (request.IsCompleted == true && !result.WasCompleted && currentUser.UserId is { } actorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                request.TaskId,
                ActivityActions.TaskChecklistItemCompleted,
                actorId,
                string.Empty,
                result.OrganizationId,
                new { itemId = result.Item.Id, title = result.Item.Title },
                cancellationToken);
        }
        if (request.IsCompleted == true && !result.WasCompleted && result.AssigneeId is { } assigneeId)
        {
            if (!result.HasIncompleteItems)
            {
                await notificationService.CreateAsync(
                    assigneeId,
                    "task.checklist_done",
                    "Checklist completed",
                    "Checklist is fully completed",
                    entityType: "Task",
                    entityId: result.TaskId,
                    ct: cancellationToken);
            }
        }

        boardCacheVersion.BumpProject(result.ProjectId);
        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, result.AssigneeId);
        return new ChecklistItemDto(
            result.Item.Id,
            result.Item.Title,
            result.Item.IsCompleted,
            result.Item.Order,
            result.Item.CompletedAtUtc);
    }
}



