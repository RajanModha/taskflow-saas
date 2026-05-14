using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class UpdateWorkspaceTagCommandHandler(IWorkspaceTagService tags)
    : IRequestHandler<UpdateWorkspaceTagCommand, (int StatusCode, object? Body)>
{
    public Task<(int StatusCode, object? Body)> Handle(
        UpdateWorkspaceTagCommand request,
        CancellationToken cancellationToken) =>
        tags.UpdateTagAsync(request.UserId, request.TagId, request.Request, cancellationToken);
}
