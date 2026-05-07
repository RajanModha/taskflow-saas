using WorkspaceRole = TaskFlow.Domain.Entities.WorkspaceRole;

namespace TaskFlow.Domain.Repositories;

public interface IWorkspaceManagementRepository
{
    Task<WorkspaceUserReadModel?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<WorkspaceUserReadModel?> GetUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<WorkspaceOrganizationReadModel?> GetOrganizationByIdAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<int> CountOrganizationMembersAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<WorkspaceMemberPageRowReadModel> Items, int Total)> GetMembersPageAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? q,
        WorkspaceRole? roleFilter,
        CancellationToken cancellationToken);

    Task<bool> OrganizationHasMemberWithNormalizedEmailAsync(Guid organizationId, string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> HasActivePendingInviteAsync(Guid organizationId, string normalizedEmail, DateTime nowUtc, CancellationToken cancellationToken);
    Task DeleteExpiredInvitesAsync(Guid organizationId, string normalizedEmail, DateTime nowUtc, CancellationToken cancellationToken);
    Task CreatePendingInviteAsync(WorkspacePendingInviteMutationInput input, CancellationToken cancellationToken);
    Task<WorkspacePendingInviteReadModel?> GetPendingInviteByEmailAsync(Guid organizationId, string normalizedEmail, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkspacePendingInviteReadModel>> ListInvitesAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<bool> CancelInviteAsync(Guid organizationId, Guid inviteId, CancellationToken cancellationToken);
    Task<WorkspacePendingInviteReadModel?> GetPendingInviteByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task UpdatePendingInviteForResendAsync(Guid inviteId, string tokenHash, DateTime lastResentAtUtc, DateTime expiresAtUtc, CancellationToken cancellationToken);
    Task MarkInviteAcceptedAsync(Guid inviteId, DateTime acceptedAtUtc, CancellationToken cancellationToken);

    Task<bool> UpdateUserWorkspaceAsync(Guid userId, Guid organizationId, WorkspaceRole role, DateTime joinedAtUtc, CancellationToken cancellationToken);
    Task<bool> UpdateUserRoleAsync(Guid userId, Guid organizationId, WorkspaceRole role, CancellationToken cancellationToken);
    Task<int> CountOwnersAsync(Guid organizationId, CancellationToken cancellationToken);
    Task UnassignTasksForMemberAsync(Guid organizationId, Guid memberId, CancellationToken cancellationToken);
    Task<bool> RemoveUserFromWorkspaceAsync(Guid userId, DateTime joinedAtUtc, CancellationToken cancellationToken);
    Task<bool> UpdateOrganizationNameAsync(Guid organizationId, string name, CancellationToken cancellationToken);
    Task<bool> UpdateOrganizationJoinCodeAsync(Guid organizationId, string joinCode, CancellationToken cancellationToken);
    Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken cancellationToken);
}
