using MediatR;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Notifications;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class GetNotificationsHandler(
    INotificationReadRepository notificationReadRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetNotificationsQuery, PagedResultDto<NotificationDto>>
{
    public async Task<PagedResultDto<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return PagedResultDto<NotificationDto>.Create([], 1, 20, 0);
        }

        var paged = await notificationReadRepository.GetPagedNotificationsAsync(
            userId,
            request.Page,
            request.PageSize,
            request.UnreadOnly,
            cancellationToken);
        var items = paged.Items
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.IsRead,
                n.CreatedAt,
                n.EntityType,
                n.EntityId))
            .ToList();
        return PagedResultDto<NotificationDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }
}
