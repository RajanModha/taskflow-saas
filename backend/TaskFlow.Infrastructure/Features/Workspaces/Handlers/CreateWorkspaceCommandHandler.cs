using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class CreateWorkspaceCommandHandler(IWorkspaceService workspaces)
    : IRequestHandler<CreateWorkspaceCommand, WorkspaceOutcome>
{
    public Task<WorkspaceOutcome> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken) =>
        workspaces.CreateAsync(request.UserId, request.Request, cancellationToken);
}
