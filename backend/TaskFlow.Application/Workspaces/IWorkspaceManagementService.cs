using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Workspaces;

public interface IWorkspaceManagementService
{
    Task<MyWorkspaceResponse?> GetMyWorkspaceAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<WorkspaceMembersPageResponse?> GetMembersPageAsync(
        Guid userId,
        int page,
        int pageSize,
        string? q,
        WorkspaceRole? roleFilter,
        CancellationToken cancellationToken = default);

    Task<(int StatusCode, object Body)> InviteMemberAsync(
        Guid actorUserId,
        InviteMemberRequest request,
        CancellationToken cancellationToken = default);

    Task<(int StatusCode, object Body)> ResendInviteAsync(
        Guid actorUserId,
        ResendInviteRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceInviteRowDto>?> ListInvitesAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<int> CancelInviteAsync(Guid actorUserId, Guid inviteId, CancellationToken cancellationToken = default);

    Task<(int StatusCode, object Body)> AcceptInviteAsync(
        AcceptInviteRequest request,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken = default);

    Task<(int StatusCode, string? Error)> UpdateMemberRoleAsync(
        Guid actorUserId,
        Guid memberId,
        UpdateMemberRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<int> RemoveMemberAsync(Guid actorUserId, Guid memberId, CancellationToken cancellationToken = default);

    Task<(int StatusCode, object? Body, string? Error)> RegenerateJoinCodeAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<(int StatusCode, object? Body, string? Error)> UpdateWorkspaceNameAsync(
        Guid actorUserId,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default);
}
