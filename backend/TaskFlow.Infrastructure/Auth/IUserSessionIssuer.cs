using TaskFlow.Application.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Auth;

/// <summary>Issues JWT access tokens and persists refresh token rows for a user.</summary>
public interface IUserSessionIssuer
{
    /// <summary>Creates a new refresh token row and returns access + refresh pair.</summary>
    Task<AuthResponse> IssueSessionAsync(
        ApplicationUser user,
        SessionConnectionInfo? connection,
        CancellationToken cancellationToken = default);

    /// <summary>Creates access JWT and a refresh row using the provided raw/hash pair (after rotation bookkeeping).</summary>
    Task<AuthResponse> AttachRefreshSessionAsync(
        ApplicationUser user,
        string refreshTokenRaw,
        string refreshTokenHash,
        DateTime refreshExpiresAtUtc,
        SessionConnectionInfo? connection,
        CancellationToken cancellationToken = default);
}
