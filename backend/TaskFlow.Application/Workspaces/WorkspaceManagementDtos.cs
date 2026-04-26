using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Workspaces;

public sealed record MyWorkspaceResponse(
    Guid Id,
    string Name,
    int MemberCount,
    string JoinCode,
    DateTimeOffset CreatedAt,
    string CurrentUserRole);

public sealed record WorkspaceMembersPageResponse(
    IReadOnlyList<WorkspaceMemberRowDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record WorkspaceMemberRowDto(
    Guid Id,
    string UserName,
    string? DisplayName,
    string Email,
    string Role,
    DateTimeOffset JoinedAt);

public sealed record InviteMemberRequest(string Email, string Role);

public sealed record InviteMemberResponse(string Message);

public sealed record ResendInviteRequest(string Email);

public sealed record ResendInviteResponse(string Message);

public sealed record WorkspaceInviteRowDto(
    Guid Id,
    string Email,
    string Role,
    DateTimeOffset SentAt,
    DateTimeOffset ExpiresAt,
    int ResendCount,
    string Status);

public sealed record AcceptInviteRequest(string Token);

public sealed record AcceptInviteJoinedResponse(string Message);

public sealed record AcceptInviteRequiresRegistrationResponse(
    bool RequiresRegistration,
    string Email,
    string WorkspaceName);

public sealed record UpdateMemberRoleRequest(string Role);

public sealed record RegenerateJoinCodeResponse(string JoinCode);

public sealed record UpdateWorkspaceRequest(string Name);

public sealed record UpdateWorkspaceResponse(string Message);

public static class WorkspaceRoleStrings
{
    public const string Owner = nameof(WorkspaceRole.Owner);
    public const string Admin = nameof(WorkspaceRole.Admin);
    public const string Member = nameof(WorkspaceRole.Member);

    public static bool TryParseInviteRole(string role, out WorkspaceRole workspaceRole)
    {
        workspaceRole = WorkspaceRole.Member;
        if (string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase))
        {
            workspaceRole = WorkspaceRole.Admin;
            return true;
        }

        if (string.Equals(role, Member, StringComparison.OrdinalIgnoreCase))
        {
            workspaceRole = WorkspaceRole.Member;
            return true;
        }

        return false;
    }

    public static bool TryParseAdminAssignableRole(string role, out WorkspaceRole workspaceRole) =>
        TryParseInviteRole(role, out workspaceRole);

    public static string ToApiString(WorkspaceRole role) => role.ToString();
}
