using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record ListWorkspaceInvitesQuery(Guid UserId) : IRequest<IReadOnlyList<WorkspaceInviteRowDto>?>;
