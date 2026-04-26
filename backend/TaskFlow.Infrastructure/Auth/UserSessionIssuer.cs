using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Auth;

public sealed class UserSessionIssuer(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator tokenGenerator,
    TimeProvider timeProvider,
    IOptions<JwtSettings> jwtSettings) : IUserSessionIssuer
{
    public async Task<AuthResponse> IssueSessionAsync(ApplicationUser user, CancellationToken cancellationToken = default)
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
            now,
            out var expiresUtc);

        var refreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var refreshDays = jwtSettings.Value.RefreshTokenDays <= 0 ? 14 : jwtSettings.Value.RefreshTokenDays;
        user.RefreshTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshRaw)));
        user.RefreshTokenExpiryUtc = now.AddDays(refreshDays);

        await userManager.UpdateAsync(user);

        return new AuthResponse(
            accessToken,
            new DateTimeOffset(expiresUtc, TimeSpan.Zero),
            "Bearer",
            refreshRaw,
            new DateTimeOffset(user.RefreshTokenExpiryUtc.Value, TimeSpan.Zero));
    }
}
