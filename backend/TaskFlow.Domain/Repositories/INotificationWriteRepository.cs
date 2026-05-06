namespace TaskFlow.Domain.Repositories;

public interface INotificationWriteRepository
{
    Task<MarkNotificationReadMutationResult> MarkNotificationReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken);

    Task<int> MarkAllNotificationsReadAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> DeleteNotificationAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken);
}
