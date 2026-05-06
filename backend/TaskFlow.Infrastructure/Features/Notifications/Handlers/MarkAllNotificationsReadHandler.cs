using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Notifications;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Notifications;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class MarkAllNotificationsReadHandler(
    INotificationWriteRepository notificationWriteRepository,
    ICurrentUser currentUser,
    IMemoryCache cache)
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return 0;
        }

        var updated = await notificationWriteRepository.MarkAllNotificationsReadAsync(userId, cancellationToken);

        if (updated > 0)
        {
            cache.Remove(NotificationCacheKeys.UnreadCount(userId));
        }

        return updated;
    }
}
