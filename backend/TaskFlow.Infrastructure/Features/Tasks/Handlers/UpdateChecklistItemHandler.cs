using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class UpdateChecklistItemHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    TimeProvider timeProvider,
    IActivityLogger activityLogger,
    INotificationService notificationService,
    IMemoryCache cache)
    : IRequestHandler<UpdateChecklistItemCommand, ChecklistItemDto?>
{
    public async Task<ChecklistItemDto?> Handle(UpdateChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(
            dbContext,
            currentTenant,
            request.TaskId,
            cancellationToken);

        if (task is null)
        {
            return null;
        }

        var item = await dbContext.ChecklistItems
            .FirstOrDefaultAsync(
                c => c.Id == request.ItemId && c.TaskId == request.TaskId,
                cancellationToken);

        if (item is null)
        {
            return null;
        }

        var wasCompleted = item.IsCompleted;

        if (request.Title is not null)
        {
            item.Title = request.Title.Trim();
        }

        if (request.IsCompleted is { } completed)
        {
            item.IsCompleted = completed;
            item.CompletedAtUtc = completed ? timeProvider.GetUtcNow().UtcDateTime : null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.IsCompleted == true && !wasCompleted && currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                request.TaskId,
                ActivityActions.TaskChecklistItemCompleted,
                actorId,
                actorName,
                task.OrganizationId,
                new { itemId = item.Id, title = item.Title },
                cancellationToken);
        }
        if (request.IsCompleted == true && !wasCompleted && task.AssigneeId is { } assigneeId)
        {
            var hasIncomplete = await dbContext.ChecklistItems
                .AsNoTracking()
                .AnyAsync(c => c.TaskId == request.TaskId && !c.IsCompleted, cancellationToken);

            if (!hasIncomplete)
            {
                await notificationService.CreateAsync(
                    assigneeId,
                    "task.checklist_done",
                    "Checklist completed",
                    $"Checklist for '{task.Title}' is fully completed",
                    entityType: "Task",
                    entityId: task.Id,
                    ct: cancellationToken);
            }
        }

        boardCacheVersion.BumpProject(task.ProjectId);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, task.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, task.AssigneeId);

        return ChecklistItemMapper.ToDto(item);
    }
}



