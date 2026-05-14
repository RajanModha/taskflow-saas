using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class ListWorkspaceWebhooksQueryHandler(IWorkspaceWebhookService webhooks)
    : IRequestHandler<ListWorkspaceWebhooksQuery, IReadOnlyList<WebhookDto>?>
{
    public Task<IReadOnlyList<WebhookDto>?> Handle(
        ListWorkspaceWebhooksQuery request,
        CancellationToken cancellationToken) =>
        webhooks.ListWebhooksAsync(request.UserId, cancellationToken);
}
