using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Repositories;

public interface INotificationReadRepository
{
    Task<PagedResult<NotificationReadModel>> GetPagedNotificationsAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
