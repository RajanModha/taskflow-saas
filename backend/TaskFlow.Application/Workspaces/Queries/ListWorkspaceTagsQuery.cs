using MediatR;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Workspaces;

public sealed record ListWorkspaceTagsQuery(Guid UserId) : IRequest<IReadOnlyList<TagDto>?>;
