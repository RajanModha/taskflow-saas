using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Notifications;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Notifications;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class MarkNotificationReadHandler(
    INotificationWriteRepository notificationWriteRepository,
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

        var result = await notificationWriteRepository.MarkNotificationReadAsync(
            userId,
            request.NotificationId,
            cancellationToken);
        if (result.MarkedAsRead)
        {
            cache.Remove(NotificationCacheKeys.UnreadCount(userId));
            return true;
        }

        return result.Exists;
    }
}
