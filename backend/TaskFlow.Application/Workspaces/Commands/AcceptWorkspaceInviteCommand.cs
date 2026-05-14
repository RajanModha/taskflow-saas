using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record AcceptWorkspaceInviteCommand(AcceptInviteRequest Request, Guid? AuthenticatedUserId)
    : IRequest<(int StatusCode, object Body)>;
