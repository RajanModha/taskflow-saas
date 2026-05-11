using System.Data;
using TaskFlow.Domain.Models.Auth;

namespace TaskFlow.Domain.Repositories;

public interface IAuthRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> UserNameExistsAsync(string normalizedUserName, CancellationToken cancellationToken);
    Task AddOrganizationAsync(TaskFlow.Domain.Entities.Organization organization, CancellationToken cancellationToken);
    Task<bool> UserNameTakenInOrganizationAsync(Guid organizationId, Guid excludedUserId, string normalizedUserName, CancellationToken cancellationToken);
    Task<TaskFlow.Domain.Entities.Organization?> GetOrganizationByIdAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<AuthRefreshTokenRecord?> GetRefreshTokenByHashAsync(string hashHex, CancellationToken cancellationToken);
    Task MarkRefreshTokenRotatedAsync(Guid refreshTokenId, DateTime revokedAtUtc, string replacedByTokenHash, CancellationToken cancellationToken);
    Task<bool> HasValidRefreshTokenAsync(Guid userId, string refreshHash, DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserSessionRow>> GetActiveSessionsAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> RevokeSessionAsync(Guid userId, Guid sessionId, DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> RevokeSessionByHashAsync(Guid userId, string refreshHash, DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> RevokeAllActiveRefreshTokensForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> RevokeOtherActiveRefreshTokensAsync(Guid userId, string keepTokenHash, DateTime nowUtc, CancellationToken cancellationToken);
    void ClearChangeTracker();
    Task<T> WithTransactionAsync<T>(
        Func<CancellationToken, Task<(bool Commit, T Result)>> action,
        CancellationToken cancellationToken,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}
