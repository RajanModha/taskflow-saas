using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Notifications;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Notifications;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class DeleteNotificationHandler(
    INotificationWriteRepository notificationWriteRepository,
    ICurrentUser currentUser,
    IMemoryCache cache)
    : IRequestHandler<DeleteNotificationCommand, bool>
{
    public async Task<bool> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var deleted = await notificationWriteRepository.DeleteNotificationAsync(
            userId,
            request.NotificationId,
            cancellationToken);
        if (deleted)
        {
            cache.Remove(NotificationCacheKeys.UnreadCount(userId));
        }

        return deleted;
    }
}
