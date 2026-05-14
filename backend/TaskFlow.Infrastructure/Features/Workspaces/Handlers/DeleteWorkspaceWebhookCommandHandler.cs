using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class DeleteWorkspaceWebhookCommandHandler(IWorkspaceWebhookService webhooks)
    : IRequestHandler<DeleteWorkspaceWebhookCommand, int>
{
    public Task<int> Handle(DeleteWorkspaceWebhookCommand request, CancellationToken cancellationToken) =>
        webhooks.DeleteWebhookAsync(request.UserId, request.WebhookId, cancellationToken);
}
