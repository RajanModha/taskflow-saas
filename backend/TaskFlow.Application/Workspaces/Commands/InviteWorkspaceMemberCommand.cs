using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record InviteWorkspaceMemberCommand(Guid UserId, InviteMemberRequest Request)
    : IRequest<(int StatusCode, object Body)>;
