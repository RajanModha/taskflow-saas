using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class ListWorkspaceInvitesQueryHandler(IWorkspaceManagementService management)
    : IRequestHandler<ListWorkspaceInvitesQuery, IReadOnlyList<WorkspaceInviteRowDto>?>
{
    public Task<IReadOnlyList<WorkspaceInviteRowDto>?> Handle(
        ListWorkspaceInvitesQuery request,
        CancellationToken cancellationToken) =>
        management.ListInvitesAsync(request.UserId, cancellationToken);
}
