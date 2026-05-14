using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class CreateWorkspaceTagCommandHandler(IWorkspaceTagService tags)
    : IRequestHandler<CreateWorkspaceTagCommand, (int StatusCode, object? Body)>
{
    public Task<(int StatusCode, object? Body)> Handle(
        CreateWorkspaceTagCommand request,
        CancellationToken cancellationToken) =>
        tags.CreateTagAsync(request.UserId, request.Request, cancellationToken);
}
