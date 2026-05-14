using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record UpdateWorkspaceWebhookCommand(
    Guid UserId,
    Guid WebhookId,
    UpdateWorkspaceWebhookRequest Request) : IRequest<(int StatusCode, object? Body)>;
