using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Common;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Auth;

public sealed class LogoutCommandHandler(TaskFlowDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<LogoutCommand>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = RefreshTokenCrypto.HashRaw(request.Request.RefreshToken.Trim());
        await dbContext.RefreshTokens
            .Where(t => t.UserId == request.UserId && t.TokenHash == hash && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, now),
                cancellationToken);
        return Unit.Value;
    }
}

public sealed class LogoutAllCommandHandler(TaskFlowDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<LogoutAllCommand>
{
    public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.RefreshTokens
            .Where(t => t.UserId == request.UserId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, now),
                cancellationToken);
        return Unit.Value;
    }
}

public sealed class GetSessionsQueryHandler(TaskFlowDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetSessionsQuery, IReadOnlyList<UserSessionItemDto>>
{
    public async Task<IReadOnlyList<UserSessionItemDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        string? currentHash = null;
        if (!string.IsNullOrWhiteSpace(request.RefreshTokenRawForCurrentMarker))
        {
            currentHash = RefreshTokenCrypto.HashRaw(request.RefreshTokenRawForCurrentMarker.Trim());
        }

        var rows = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == request.UserId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.DeviceInfo,
                t.IpAddress,
                t.CreatedAtUtc,
                t.ExpiresAtUtc,
                t.TokenHash,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(t => new UserSessionItemDto(
                t.Id,
                t.DeviceInfo,
                t.IpAddress,
                new DateTimeOffset(t.CreatedAtUtc, TimeSpan.Zero),
                new DateTimeOffset(t.ExpiresAtUtc, TimeSpan.Zero),
                currentHash is not null && TokenHashesEqual(t.TokenHash, currentHash)))
            .ToList();
    }

    private static bool TokenHashesEqual(string? storedHex, string computedHex)
    {
        if (storedHex is null || storedHex.Length != computedHex.Length)
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(storedHex),
                Convert.FromHexString(computedHex));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class TryRevokeSessionCommandHandler(TaskFlowDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<TryRevokeSessionCommand, bool>
{
    public async Task<bool> Handle(TryRevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var affected = await dbContext.RefreshTokens
            .Where(t => t.Id == request.SessionId && t.UserId == request.UserId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, now),
                cancellationToken);
        return affected > 0;
    }
}

public sealed class GetProfileQueryHandler(
    TaskFlowDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IRequestHandler<GetProfileQuery, UserProfileResponse?>
{
    public async Task<UserProfileResponse?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return await MapToProfileResponseAsync(user, cancellationToken);
    }

    private async Task<UserProfileResponse> MapToProfileResponseAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (user.OrganizationId == Guid.Empty)
        {
            var emptyRoles = Array.Empty<string>();
            return new UserProfileResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty,
                emptyRoles,
                PickPrimaryRole(emptyRoles),
                Guid.Empty,
                string.Empty,
                string.Empty,
                user.DisplayName,
                user.AvatarUrl,
                new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero));
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var organization = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == user.OrganizationId, cancellationToken);

        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles,
            PickPrimaryRole(roles),
            user.OrganizationId,
            organization?.Name ?? string.Empty,
            organization?.JoinCode ?? string.Empty,
            user.DisplayName,
            user.AvatarUrl,
            new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero));
    }

    private static string PickPrimaryRole(IReadOnlyList<string> roles)
    {
        foreach (var role in roles)
        {
            if (string.Equals(role, DomainRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return DomainRoles.Admin;
            }
        }

        return roles.Count > 0 ? roles[0] : DomainRoles.User;
    }
}
