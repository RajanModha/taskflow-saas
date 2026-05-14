using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class ResendWorkspaceInviteCommandHandler(IWorkspaceManagementService management)
    : IRequestHandler<ResendWorkspaceInviteCommand, (int StatusCode, object Body)>
{
    public Task<(int StatusCode, object Body)> Handle(
        ResendWorkspaceInviteCommand request,
        CancellationToken cancellationToken) =>
        management.ResendInviteAsync(request.UserId, request.Request, cancellationToken);
}
