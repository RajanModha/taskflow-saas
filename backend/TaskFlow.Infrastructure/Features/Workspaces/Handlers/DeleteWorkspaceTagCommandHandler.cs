using MediatR;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class DeleteWorkspaceTagCommandHandler(IWorkspaceTagService tags)
    : IRequestHandler<DeleteWorkspaceTagCommand, int>
{
    public Task<int> Handle(DeleteWorkspaceTagCommand request, CancellationToken cancellationToken) =>
        tags.DeleteTagAsync(request.UserId, request.TagId, cancellationToken);
}
