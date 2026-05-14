using MediatR;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
