using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class JoinWorkspaceCommandHandler(IWorkspaceService workspaces)
    : IRequestHandler<JoinWorkspaceCommand, WorkspaceOutcome>
{
    public Task<WorkspaceOutcome> Handle(JoinWorkspaceCommand request, CancellationToken cancellationToken) =>
        workspaces.JoinAsync(request.UserId, request.Request, cancellationToken);
}
