namespace TaskFlow.Domain.Repositories;

public sealed record NotificationReadModel(
    Guid Id,
    Guid UserId,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt,
    string? EntityType,
    Guid? EntityId);

public sealed record MarkNotificationReadMutationResult(
    bool Exists,
    bool MarkedAsRead);
