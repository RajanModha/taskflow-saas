using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record ResendWorkspaceInviteCommand(Guid UserId, ResendInviteRequest Request)
    : IRequest<(int StatusCode, object Body)>;
