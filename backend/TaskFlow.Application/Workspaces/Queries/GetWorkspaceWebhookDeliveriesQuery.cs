using MediatR;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.Workspaces;

public sealed record GetWorkspaceWebhookDeliveriesQuery(
    Guid UserId,
    Guid WebhookId,
    int Page,
    int PageSize) : IRequest<PagedResultDto<WebhookDeliveryLogDto>?>;
