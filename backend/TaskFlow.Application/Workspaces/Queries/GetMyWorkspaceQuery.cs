using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record GetMyWorkspaceQuery(Guid UserId) : IRequest<MyWorkspaceResponse?>;
