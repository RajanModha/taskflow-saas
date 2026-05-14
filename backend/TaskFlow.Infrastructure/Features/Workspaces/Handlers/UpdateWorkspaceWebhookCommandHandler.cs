using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class UpdateWorkspaceWebhookCommandHandler(IWorkspaceWebhookService webhooks)
    : IRequestHandler<UpdateWorkspaceWebhookCommand, (int StatusCode, object? Body)>
{
    public Task<(int StatusCode, object? Body)> Handle(
        UpdateWorkspaceWebhookCommand request,
        CancellationToken cancellationToken) =>
        webhooks.UpdateWebhookAsync(request.UserId, request.WebhookId, request.Request, cancellationToken);
}
