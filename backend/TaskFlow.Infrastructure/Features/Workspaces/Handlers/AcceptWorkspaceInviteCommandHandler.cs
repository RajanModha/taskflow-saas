using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class AcceptWorkspaceInviteCommandHandler(IWorkspaceManagementService management)
    : IRequestHandler<AcceptWorkspaceInviteCommand, (int StatusCode, object Body)>
{
    public Task<(int StatusCode, object Body)> Handle(
        AcceptWorkspaceInviteCommand request,
        CancellationToken cancellationToken) =>
        management.AcceptInviteAsync(request.Request, request.AuthenticatedUserId, cancellationToken);
}
