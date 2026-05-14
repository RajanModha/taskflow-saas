using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record CancelWorkspaceInviteCommand(Guid UserId, Guid InviteId) : IRequest<int>;
