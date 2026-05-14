using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record UpdateWorkspaceMemberRoleCommand(
    Guid UserId,
    Guid MemberId,
    UpdateMemberRoleRequest Request) : IRequest<(int StatusCode, string? Error)>;
