using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Notifications;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Notifications;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class GetUnreadNotificationsCountHandler(
    INotificationReadRepository notificationReadRepository,
    ICurrentUser currentUser,
    IMemoryCache cache)
    : IRequestHandler<GetUnreadNotificationsCountQuery, int>
{
    public async Task<int> Handle(GetUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return 0;
        }

        var key = NotificationCacheKeys.UnreadCount(userId);
        return await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
            return await notificationReadRepository.GetUnreadCountAsync(userId, cancellationToken);
        });
    }
}

