using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record DeleteWorkspaceWebhookCommand(Guid UserId, Guid WebhookId) : IRequest<int>;
