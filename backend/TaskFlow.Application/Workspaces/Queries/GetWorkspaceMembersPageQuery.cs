using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record GetWorkspaceMembersPageQuery(
    Guid UserId,
    int Page,
    int PageSize,
    string? Q,
    string? Role) : IRequest<WorkspaceMembersPageOutcome>;
