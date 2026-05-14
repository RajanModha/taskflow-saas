using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class UpdateWorkspaceMemberRoleCommandHandler(IWorkspaceManagementService management)
    : IRequestHandler<UpdateWorkspaceMemberRoleCommand, (int StatusCode, string? Error)>
{
    public Task<(int StatusCode, string? Error)> Handle(
        UpdateWorkspaceMemberRoleCommand request,
        CancellationToken cancellationToken) =>
        management.UpdateMemberRoleAsync(request.UserId, request.MemberId, request.Request, cancellationToken);
}
