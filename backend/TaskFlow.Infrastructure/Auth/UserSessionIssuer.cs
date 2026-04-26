using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Auth;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Auth;

public sealed class UserSessionIssuer(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator tokenGenerator,
    TimeProvider timeProvider,
    IOptions<JwtSettings> jwtSettings,
    TaskFlowDbContext dbContext) : IUserSessionIssuer
{
    public async Task<AuthResponse> IssueSessionAsync(
        ApplicationUser user,
        SessionConnectionInfo? connection,
        CancellationToken cancellationToken = default)
    {
        if (user.OrganizationId == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot issue a session for a user without an organization.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var refreshDays = jwtSettings.Value.RefreshTokenDays <= 0 ? 30 : jwtSettings.Value.RefreshTokenDays;
        var expiresAtUtc = now.AddDays(refreshDays);
        var (raw, hash) = RefreshTokenCrypto.GenerateToken();
        return await AttachRefreshSessionAsync(user, raw, hash, expiresAtUtc, connection, cancellationToken);
    }

    public async Task<AuthResponse> AttachRefreshSessionAsync(
        ApplicationUser user,
        string refreshTokenRaw,
        string refreshTokenHash,
        DateTime refreshExpiresAtUtc,
        SessionConnectionInfo? connection,
        CancellationToken cancellationToken = default)
    {
        if (user.OrganizationId == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot issue a session for a user without an organization.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenGenerator.CreateAccessToken(
            user.Id,
            user.Email ?? string.Empty,
            roles,
            user.OrganizationId,
            user.WorkspaceRole,
            now,
            out var expiresUtc);

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpiresAtUtc,
            DeviceInfo = connection?.UserAgent,
            IpAddress = connection?.IpAddress,
        };

        await dbContext.RefreshTokens.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            new DateTimeOffset(expiresUtc, TimeSpan.Zero),
            "Bearer",
            refreshTokenRaw,
            new DateTimeOffset(refreshExpiresAtUtc, TimeSpan.Zero));
    }
}
