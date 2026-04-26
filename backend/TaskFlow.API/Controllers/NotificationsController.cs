using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;
using TaskFlow.Application.Notifications;

namespace TaskFlow.API.Controllers;

/// <summary>Manage in-app notifications for the authenticated user.</summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    public sealed record UnreadCountResponse(int Count);

    public sealed record ReadAllResponse(int UpdatedCount);

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<NotificationDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetNotificationsQuery(page, pageSize, unreadOnly), cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountResponse>> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var count = await mediator.Send(new GetUnreadNotificationsCountQuery(), cancellationToken);
        return Ok(new UnreadCountResponse(count));
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken = default)
    {
        var ok = await mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return ok ? Ok() : NotFound();
    }

    [HttpPut("read-all")]
    [ProducesResponseType(typeof(ReadAllResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReadAllResponse>> MarkAllRead(CancellationToken cancellationToken = default)
    {
        var updatedCount = await mediator.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return Ok(new ReadAllResponse(updatedCount));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var ok = await mediator.Send(new DeleteNotificationCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
