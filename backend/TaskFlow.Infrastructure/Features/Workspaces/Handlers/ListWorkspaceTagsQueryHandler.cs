using MediatR;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.Infrastructure.Features.Workspaces.Handlers;

public sealed class ListWorkspaceTagsQueryHandler(IWorkspaceTagService tags)
    : IRequestHandler<ListWorkspaceTagsQuery, IReadOnlyList<TagDto>?>
{
    public Task<IReadOnlyList<TagDto>?> Handle(ListWorkspaceTagsQuery request, CancellationToken cancellationToken) =>
        tags.ListTagsAsync(request.UserId, cancellationToken);
}
