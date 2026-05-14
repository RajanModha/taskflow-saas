using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record TestWorkspaceWebhookCommand(Guid UserId, Guid WebhookId)
    : IRequest<(int StatusCode, object? Body)>;
