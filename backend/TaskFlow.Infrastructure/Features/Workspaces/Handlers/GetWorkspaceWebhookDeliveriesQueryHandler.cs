using MediatR;
using TaskFlow.Application.Common;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class GetWorkspaceWebhookDeliveriesQueryHandler(IWorkspaceWebhookService webhooks)
    : IRequestHandler<GetWorkspaceWebhookDeliveriesQuery, PagedResultDto<WebhookDeliveryLogDto>?>
{
    public Task<PagedResultDto<WebhookDeliveryLogDto>?> Handle(
        GetWorkspaceWebhookDeliveriesQuery request,
        CancellationToken cancellationToken) =>
        webhooks.GetDeliveriesPageAsync(
            request.UserId,
            request.WebhookId,
            request.Page,
            request.PageSize,
            cancellationToken);
}
