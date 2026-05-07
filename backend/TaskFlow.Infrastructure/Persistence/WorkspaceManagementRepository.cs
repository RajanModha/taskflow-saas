using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using Task = System.Threading.Tasks.Task;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class WorkspaceManagementRepository(TaskFlowDbContext dbContext) : IWorkspaceManagementRepository
{
    public async Task<WorkspaceUserReadModel?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new WorkspaceUserReadModel(
                u.Id,
                u.OrganizationId,
                u.WorkspaceRole,
                u.WorkspaceJoinedAtUtc,
                u.UserName,
                u.DisplayName,
                u.Email,
                u.NormalizedEmail))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<WorkspaceUserReadModel?> GetUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.NormalizedEmail == normalizedEmail)
            .Select(u => new WorkspaceUserReadModel(
                u.Id,
                u.OrganizationId,
                u.WorkspaceRole,
                u.WorkspaceJoinedAtUtc,
                u.UserName,
                u.DisplayName,
                u.Email,
                u.NormalizedEmail))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<WorkspaceOrganizationReadModel?> GetOrganizationByIdAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.Organizations
            .AsNoTracking()
            .Where(o => o.Id == organizationId)
            .Select(o => new WorkspaceOrganizationReadModel(o.Id, o.Name, o.JoinCode, o.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> CountOrganizationMembersAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.OrganizationId == organizationId, cancellationToken);

    public async Task<(IReadOnlyList<WorkspaceMemberPageRowReadModel> Items, int Total)> GetMembersPageAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? q,
        WorkspaceRole? roleFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.OrganizationId == organizationId);

        if (roleFilter is not null)
        {
            query = query.Where(u => u.WorkspaceRole == roleFilter);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var normalized = term.ToUpperInvariant();
            query = query.Where(u =>
                (u.NormalizedUserName != null && u.NormalizedUserName.Contains(normalized)) ||
                (u.NormalizedEmail != null && u.NormalizedEmail.Contains(normalized)) ||
                (u.DisplayName != null && EF.Functions.ILike(u.DisplayName, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new WorkspaceMemberPageRowReadModel(
                u.Id,
                u.UserName ?? string.Empty,
                u.DisplayName,
                u.Email ?? string.Empty,
                u.WorkspaceRole,
                u.WorkspaceJoinedAtUtc))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<bool> OrganizationHasMemberWithNormalizedEmailAsync(Guid organizationId, string normalizedEmail, CancellationToken cancellationToken) =>
        await dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.OrganizationId == organizationId && u.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<bool> HasActivePendingInviteAsync(Guid organizationId, string normalizedEmail, DateTime nowUtc, CancellationToken cancellationToken) =>
        await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .AnyAsync(i => i.OrganizationId == organizationId &&
                           i.NormalizedEmail == normalizedEmail &&
                           i.AcceptedAtUtc == null &&
                           i.ExpiresAtUtc > nowUtc, cancellationToken);

    public async Task DeleteExpiredInvitesAsync(Guid organizationId, string normalizedEmail, DateTime nowUtc, CancellationToken cancellationToken) =>
        await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .Where(e => e.OrganizationId == organizationId &&
                        e.NormalizedEmail == normalizedEmail &&
                        e.AcceptedAtUtc == null &&
                        e.ExpiresAtUtc <= nowUtc)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task CreatePendingInviteAsync(WorkspacePendingInviteMutationInput input, CancellationToken cancellationToken)
    {
        dbContext.PendingInvites.Add(new PendingInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = input.OrganizationId,
            Email = input.Email,
            NormalizedEmail = input.NormalizedEmail,
            Role = input.Role,
            TokenHash = input.TokenHash,
            ExpiresAtUtc = input.ExpiresAtUtc,
            SentAtUtc = input.SentAtUtc,
            ResendCount = input.ResendCount,
            LastResentAtUtc = input.LastResentAtUtc,
            AcceptedAtUtc = input.AcceptedAtUtc,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkspacePendingInviteReadModel?> GetPendingInviteByEmailAsync(Guid organizationId, string normalizedEmail, CancellationToken cancellationToken) =>
        await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .Where(i => i.OrganizationId == organizationId && i.NormalizedEmail == normalizedEmail && i.AcceptedAtUtc == null)
            .Select(i => new WorkspacePendingInviteReadModel(
                i.Id,
                i.OrganizationId,
                i.Email,
                i.NormalizedEmail,
                i.Role,
                i.TokenHash,
                i.ExpiresAtUtc,
                i.SentAtUtc,
                i.ResendCount,
                i.LastResentAtUtc,
                i.AcceptedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkspacePendingInviteReadModel>> ListInvitesAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.OrganizationId == organizationId)
            .OrderByDescending(i => i.SentAtUtc)
            .Select(i => new WorkspacePendingInviteReadModel(
                i.Id,
                i.OrganizationId,
                i.Email,
                i.NormalizedEmail,
                i.Role,
                i.TokenHash,
                i.ExpiresAtUtc,
                i.SentAtUtc,
                i.ResendCount,
                i.LastResentAtUtc,
                i.AcceptedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<bool> CancelInviteAsync(Guid organizationId, Guid inviteId, CancellationToken cancellationToken)
    {
        var deleted = await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .Where(i => i.Id == inviteId && i.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    public async Task<WorkspacePendingInviteReadModel?> GetPendingInviteByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .Where(i => i.TokenHash == tokenHash)
            .Select(i => new WorkspacePendingInviteReadModel(
                i.Id,
                i.OrganizationId,
                i.Email,
                i.NormalizedEmail,
                i.Role,
                i.TokenHash,
                i.ExpiresAtUtc,
                i.SentAtUtc,
                i.ResendCount,
                i.LastResentAtUtc,
                i.AcceptedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task UpdatePendingInviteForResendAsync(
        Guid inviteId,
        string tokenHash,
        DateTime lastResentAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken) =>
        await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .Where(i => i.Id == inviteId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(i => i.TokenHash, tokenHash)
                    .SetProperty(i => i.ResendCount, i => i.ResendCount + 1)
                    .SetProperty(i => i.LastResentAtUtc, lastResentAtUtc)
                    .SetProperty(i => i.ExpiresAtUtc, expiresAtUtc),
                cancellationToken);

    public async Task MarkInviteAcceptedAsync(Guid inviteId, DateTime acceptedAtUtc, CancellationToken cancellationToken) =>
        await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .Where(i => i.Id == inviteId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.AcceptedAtUtc, acceptedAtUtc), cancellationToken);

    public async Task<bool> UpdateUserWorkspaceAsync(Guid userId, Guid organizationId, WorkspaceRole role, DateTime joinedAtUtc, CancellationToken cancellationToken)
    {
        var updated = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(u => u.OrganizationId, organizationId)
                    .SetProperty(u => u.WorkspaceRole, role)
                    .SetProperty(u => u.WorkspaceJoinedAtUtc, joinedAtUtc),
                cancellationToken);
        return updated > 0;
    }

    public async Task<bool> UpdateUserRoleAsync(Guid userId, Guid organizationId, WorkspaceRole role, CancellationToken cancellationToken)
    {
        var updated = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == userId && u.OrganizationId == organizationId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.WorkspaceRole, role), cancellationToken);
        return updated > 0;
    }

    public async Task<int> CountOwnersAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.OrganizationId == organizationId && u.WorkspaceRole == WorkspaceRole.Owner, cancellationToken);

    public async Task UnassignTasksForMemberAsync(Guid organizationId, Guid memberId, CancellationToken cancellationToken) =>
        await dbContext.Tasks
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organizationId && t.AssigneeId == memberId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.AssigneeId, (Guid?)null), cancellationToken);

    public async Task<bool> RemoveUserFromWorkspaceAsync(Guid userId, DateTime joinedAtUtc, CancellationToken cancellationToken)
    {
        var updated = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(u => u.OrganizationId, Guid.Empty)
                    .SetProperty(u => u.WorkspaceRole, WorkspaceRole.Member)
                    .SetProperty(u => u.WorkspaceJoinedAtUtc, joinedAtUtc),
                cancellationToken);
        return updated > 0;
    }

    public async Task<bool> UpdateOrganizationNameAsync(Guid organizationId, string name, CancellationToken cancellationToken)
    {
        var updated = await dbContext.Organizations
            .Where(o => o.Id == organizationId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Name, name), cancellationToken);
        return updated > 0;
    }

    public async Task<bool> UpdateOrganizationJoinCodeAsync(Guid organizationId, string joinCode, CancellationToken cancellationToken)
    {
        var updated = await dbContext.Organizations
            .Where(o => o.Id == organizationId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.JoinCode, joinCode), cancellationToken);
        return updated > 0;
    }

    public async Task<bool> JoinCodeExistsAsync(string joinCode, CancellationToken cancellationToken) =>
        await dbContext.Organizations.AsNoTracking().AnyAsync(o => o.JoinCode == joinCode, cancellationToken);

}
