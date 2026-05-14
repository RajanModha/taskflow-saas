using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record RemoveWorkspaceMemberCommand(Guid UserId, Guid MemberId) : IRequest<int>;
