using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Auth;

public sealed class LogoutCommandHandler(IAuthRepository authRepository, TimeProvider timeProvider) : IRequestHandler<LogoutCommand>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = RefreshTokenCrypto.HashRaw(request.Request.RefreshToken.Trim());
        await authRepository.RevokeSessionByHashAsync(request.UserId, hash, now, cancellationToken);
        return Unit.Value;
    }
}

public sealed class LogoutAllCommandHandler(IAuthRepository authRepository, TimeProvider timeProvider) : IRequestHandler<LogoutAllCommand>
{
    public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await authRepository.RevokeAllActiveRefreshTokensForUserAsync(request.UserId, now, cancellationToken);
        return Unit.Value;
    }
}

public sealed class GetSessionsQueryHandler(IAuthRepository authRepository, TimeProvider timeProvider)
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

        var rows = await authRepository.GetActiveSessionsAsync(request.UserId, now, cancellationToken);
        return rows
            .Select(t => new UserSessionItemDto(
                t.Id,
                t.DeviceInfo,
                t.IpAddress,
                new DateTimeOffset(t.CreatedAtUtc, TimeSpan.Zero),
                new DateTimeOffset(t.ExpiresAtUtc, TimeSpan.Zero),
                currentHash is not null && AuthRequestCommon.TokenHashesEqual(t.TokenHash, currentHash)))
            .ToList();
    }
}

public sealed class TryRevokeSessionCommandHandler(IAuthRepository authRepository, TimeProvider timeProvider)
    : IRequestHandler<TryRevokeSessionCommand, bool>
{
    public async Task<bool> Handle(TryRevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var affected = await authRepository.RevokeSessionAsync(request.UserId, request.SessionId, now, cancellationToken);
        return affected > 0;
    }
}

public sealed class GetProfileQueryHandler(
    IAuthRepository authRepository,
    UserManager<ApplicationUser> userManager) : IRequestHandler<GetProfileQuery, UserProfileResponse?>
{
    public async Task<UserProfileResponse?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return await AuthRequestCommon.MapToProfileResponseAsync(authRepository, userManager, user, cancellationToken);
    }
}
