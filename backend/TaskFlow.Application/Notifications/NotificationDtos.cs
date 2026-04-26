using MediatR;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt,
    string? EntityType,
    Guid? EntityId);

public sealed record GetNotificationsQuery(int Page, int PageSize, bool UnreadOnly)
    : IRequest<PagedResultDto<NotificationDto>>;

public sealed record GetUnreadNotificationsCountQuery : IRequest<int>;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<bool>;

public sealed record MarkAllNotificationsReadCommand : IRequest<int>;

public sealed record DeleteNotificationCommand(Guid NotificationId) : IRequest<bool>;
