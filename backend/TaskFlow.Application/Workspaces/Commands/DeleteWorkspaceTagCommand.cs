using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record DeleteWorkspaceTagCommand(Guid UserId, Guid TagId) : IRequest<int>;
