using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class CancelWorkspaceInviteCommandHandler(IWorkspaceManagementService management)
    : IRequestHandler<CancelWorkspaceInviteCommand, int>
{
    public Task<int> Handle(CancelWorkspaceInviteCommand request, CancellationToken cancellationToken) =>
        management.CancelInviteAsync(request.UserId, request.InviteId, cancellationToken);
}
