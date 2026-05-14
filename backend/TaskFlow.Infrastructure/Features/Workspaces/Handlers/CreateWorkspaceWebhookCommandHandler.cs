using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class CreateWorkspaceWebhookCommandHandler(IWorkspaceWebhookService webhooks)
    : IRequestHandler<CreateWorkspaceWebhookCommand, (int StatusCode, object? Body)>
{
    public Task<(int StatusCode, object? Body)> Handle(
        CreateWorkspaceWebhookCommand request,
        CancellationToken cancellationToken) =>
        webhooks.CreateWebhookAsync(request.UserId, request.Request, cancellationToken);
}
