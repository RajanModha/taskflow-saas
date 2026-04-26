using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Notifications;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class GetUnreadNotificationsCountHandler(
    TaskFlowDbContext dbContext,
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
            return await dbContext.Notifications.AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        });
    }
}

