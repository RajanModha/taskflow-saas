using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class InviteWorkspaceMemberCommandHandler(IWorkspaceManagementService management)
    : IRequestHandler<InviteWorkspaceMemberCommand, (int StatusCode, object Body)>
{
    public Task<(int StatusCode, object Body)> Handle(
        InviteWorkspaceMemberCommand request,
        CancellationToken cancellationToken) =>
        management.InviteMemberAsync(request.UserId, request.Request, cancellationToken);
}
