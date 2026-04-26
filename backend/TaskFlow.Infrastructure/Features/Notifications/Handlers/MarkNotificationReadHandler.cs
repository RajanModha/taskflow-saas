using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Notifications;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class MarkNotificationReadHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache)
    : IRequestHandler<MarkNotificationReadCommand, bool>
{
    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var updated = await dbContext.Notifications
            .Where(n => n.Id == request.NotificationId && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

        if (updated > 0)
        {
            cache.Remove(NotificationCacheKeys.UnreadCount(userId));
            return true;
        }

        return await dbContext.Notifications
            .AsNoTracking()
            .AnyAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken);
    }
}
