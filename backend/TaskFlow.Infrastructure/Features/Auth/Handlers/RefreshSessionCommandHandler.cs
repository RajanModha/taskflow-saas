using System.Data;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

public sealed class RefreshSessionCommandHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    IOptions<JwtSettings> jwtSettings,
    UserManager<ApplicationUser> userManager,
    IUserSessionIssuer sessionIssuer,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RefreshSessionCommandHandler> logger) : IRequestHandler<RefreshSessionCommand, RefreshSessionOutcome>
{
    private readonly JwtSettings _jwt = jwtSettings.Value;

    public async Task<RefreshSessionOutcome> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await RefreshSessionOnceAsync(command.Request, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts - 1 && IsPostgresTransientConcurrency(ex))
            {
                authRepository.ClearChangeTracker();
                logger.LogWarning(
                    ex,
                    "Refresh token rotation hit retriable DB concurrency (attempt {Attempt} of {Max}).",
                    attempt + 1,
                    maxAttempts);
            }
        }

        return new RefreshSessionFailed(
            "Service temporarily unavailable",
            "Could not refresh the session. Please try again.",
            StatusCodes.Status503ServiceUnavailable);
    }

    private async Task<RefreshSessionOutcome> RefreshSessionOnceAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var raw = request.RefreshToken.Trim();
        var hashHex = RefreshTokenCrypto.HashRaw(raw);

        return await authRepository.WithTransactionAsync(async ct =>
        {
            var stored = await authRepository.GetRefreshTokenByHashAsync(hashHex, ct);

            if (stored is null || !AuthRequestCommon.TokenHashesEqual(stored.TokenHash, hashHex))
            {
                return (false, (RefreshSessionOutcome)new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized));
            }

            var user = await userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);
            if (user is null || !user.EmailVerified || user.OrganizationId == Guid.Empty)
            {
                return (false, (RefreshSessionOutcome)new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized));
            }

            if (stored.RevokedAtUtc.HasValue)
            {
                await authRepository.RevokeAllActiveRefreshTokensForUserAsync(user.Id, now, ct);
                return (true, (RefreshSessionOutcome)new RefreshSessionReuseDetected());
            }

            if (stored.ExpiresAtUtc <= now)
            {
                return (false, (RefreshSessionOutcome)new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized));
            }

            var (rawNew, hashNew) = RefreshTokenCrypto.GenerateToken();
            var refreshDays = _jwt.RefreshTokenDays <= 0 ? 30 : _jwt.RefreshTokenDays;
            var newExpiryUtc = now.AddDays(refreshDays);

            await authRepository.MarkRefreshTokenRotatedAsync(stored.Id, now, hashNew, ct);

            AuthResponse response;
            try
            {
                response = await sessionIssuer.AttachRefreshSessionAsync(
                    user,
                    rawNew,
                    hashNew,
                    newExpiryUtc,
                    AuthRequestCommon.GetSessionConnectionInfo(httpContextAccessor),
                    ct);
            }
            catch (InvalidOperationException)
            {
                return (false, (RefreshSessionOutcome)new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized));
            }
            return (true, (RefreshSessionOutcome)new RefreshSessionSucceeded(response));
        }, cancellationToken, IsolationLevel.Serializable);
    }

    private static bool IsPostgresTransientConcurrency(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pe && (pe.SqlState == "40001" || pe.SqlState == "40P01"))
            {
                return true;
            }
        }

        return false;
    }
}
