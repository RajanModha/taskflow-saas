using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class NotificationRepository(TaskFlowDbContext dbContext)
    : INotificationReadRepository, INotificationWriteRepository
{
    public async Task<PagedResult<NotificationReadModel>> GetPagedNotificationsAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var skip = (page - 1) * pageSize;

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(n => new NotificationReadModel(
                n.Id,
                n.UserId,
                n.Type,
                n.Title,
                n.Body,
                n.IsRead,
                n.CreatedAt,
                n.EntityType,
                n.EntityId))
            .ToListAsync(cancellationToken);
        return new PagedResult<NotificationReadModel>(items, page, pageSize, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task<MarkNotificationReadMutationResult> MarkNotificationReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
        if (updated > 0)
        {
            return new MarkNotificationReadMutationResult(true, true);
        }

        var exists = await dbContext.Notifications
            .AsNoTracking()
            .AnyAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        return new MarkNotificationReadMutationResult(exists, false);
    }

    public async Task<int> MarkAllNotificationsReadAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

    public async Task<bool> DeleteNotificationAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var deleted = await dbContext.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
