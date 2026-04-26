using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Notifications;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class DeleteNotificationHandler(
    TaskFlowDbContext dbContext,
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

        var existing = await dbContext.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var deleted = await dbContext.Notifications
            .Where(n => n.Id == request.NotificationId && n.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            cache.Remove(NotificationCacheKeys.UnreadCount(userId));
        }

        return deleted > 0;
    }
}
