namespace TaskFlow.Infrastructure.Notifications;

internal static class NotificationCacheKeys
{
    internal static string UnreadCount(Guid userId) => $"notifications:unread:{userId}";
}
