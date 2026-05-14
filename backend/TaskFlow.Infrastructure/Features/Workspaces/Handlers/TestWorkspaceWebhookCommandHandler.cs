using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class TestWorkspaceWebhookCommandHandler(IWorkspaceWebhookService webhooks)
    : IRequestHandler<TestWorkspaceWebhookCommand, (int StatusCode, object? Body)>
{
    public Task<(int StatusCode, object? Body)> Handle(
        TestWorkspaceWebhookCommand request,
        CancellationToken cancellationToken) =>
        webhooks.TestWebhookAsync(request.UserId, request.WebhookId, cancellationToken);
}
