using System.Data;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Models.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Auth;

public sealed class AuthRepository(TaskFlowDbContext dbContext) : IAuthRepository
{
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<bool> UserNameExistsAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);

    public Task AddOrganizationAsync(TaskFlow.Domain.Entities.Organization organization, CancellationToken cancellationToken) =>
        dbContext.Organizations.AddAsync(organization, cancellationToken).AsTask();

    public Task<bool> UserNameTakenInOrganizationAsync(
        Guid organizationId,
        Guid excludedUserId,
        string normalizedUserName,
        CancellationToken cancellationToken) =>
        dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(
                u => u.OrganizationId == organizationId &&
                     u.Id != excludedUserId &&
                     u.NormalizedUserName == normalizedUserName,
                cancellationToken);

    public Task<TaskFlow.Domain.Entities.Organization?> GetOrganizationByIdAsync(Guid organizationId, CancellationToken cancellationToken) =>
        dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

    public async Task<AuthRefreshTokenRecord?> GetRefreshTokenByHashAsync(string hashHex, CancellationToken cancellationToken)
    {
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == hashHex)
            .Select(t => new AuthRefreshTokenRecord(t.Id, t.UserId, t.TokenHash, t.ExpiresAtUtc, t.RevokedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task MarkRefreshTokenRotatedAsync(
        Guid refreshTokenId,
        DateTime revokedAtUtc,
        string replacedByTokenHash,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(t => t.Id == refreshTokenId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.RevokedAtUtc, revokedAtUtc)
                    .SetProperty(t => t.ReplacedByTokenHash, replacedByTokenHash),
                cancellationToken);

    public Task<bool> HasValidRefreshTokenAsync(Guid userId, string refreshHash, DateTime nowUtc, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .AsNoTracking()
            .AnyAsync(
                t => t.UserId == userId &&
                     t.TokenHash == refreshHash &&
                     t.RevokedAtUtc == null &&
                     t.ExpiresAtUtc > nowUtc,
                cancellationToken);

    public async Task<IReadOnlyList<UserSessionRow>> GetActiveSessionsAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        return await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > nowUtc)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new UserSessionRow(
                t.Id,
                t.DeviceInfo,
                t.IpAddress,
                t.CreatedAtUtc,
                t.ExpiresAtUtc,
                t.TokenHash))
            .ToListAsync(cancellationToken);
    }

    public Task<int> RevokeSessionAsync(Guid userId, Guid sessionId, DateTime nowUtc, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(t => t.Id == sessionId && t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, nowUtc),
                cancellationToken);

    public Task<int> RevokeSessionByHashAsync(Guid userId, string refreshHash, DateTime nowUtc, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenHash == refreshHash && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, nowUtc),
                cancellationToken);

    public Task<int> RevokeAllActiveRefreshTokensForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, nowUtc),
                cancellationToken);

    public Task<int> RevokeOtherActiveRefreshTokensAsync(Guid userId, string keepTokenHash, DateTime nowUtc, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.TokenHash != keepTokenHash)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, nowUtc),
                cancellationToken);

    public void ClearChangeTracker() => dbContext.ChangeTracker.Clear();

    public async Task<T> WithTransactionAsync<T>(
        Func<CancellationToken, Task<(bool Commit, T Result)>> action,
        CancellationToken cancellationToken,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        try
        {
            var (commit, result) = await action(cancellationToken);
            if (commit)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
