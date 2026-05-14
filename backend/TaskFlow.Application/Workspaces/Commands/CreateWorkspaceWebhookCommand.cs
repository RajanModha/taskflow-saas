using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record CreateWorkspaceWebhookCommand(Guid UserId, CreateWorkspaceWebhookRequest Request)
    : IRequest<(int StatusCode, object? Body)>;
