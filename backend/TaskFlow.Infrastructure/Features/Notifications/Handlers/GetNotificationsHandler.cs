using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Notifications.Handlers;

public sealed class GetNotificationsHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser)
    : IRequestHandler<GetNotificationsQuery, PagedResultDto<NotificationDto>>
{
    public async Task<PagedResultDto<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return new PagedResultDto<NotificationDto>([], 1, 20, 0);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var skip = (page - 1) * pageSize;

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.IsRead,
                n.CreatedAt,
                n.EntityType,
                n.EntityId))
            .ToListAsync(cancellationToken);

        return new PagedResultDto<NotificationDto>(items, page, pageSize, total);
    }
}
