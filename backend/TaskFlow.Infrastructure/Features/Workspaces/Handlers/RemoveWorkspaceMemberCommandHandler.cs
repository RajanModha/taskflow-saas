using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class RemoveWorkspaceMemberCommandHandler(IWorkspaceManagementService management)
    : IRequestHandler<RemoveWorkspaceMemberCommand, int>
{
    public Task<int> Handle(RemoveWorkspaceMemberCommand request, CancellationToken cancellationToken) =>
        management.RemoveMemberAsync(request.UserId, request.MemberId, cancellationToken);
}
