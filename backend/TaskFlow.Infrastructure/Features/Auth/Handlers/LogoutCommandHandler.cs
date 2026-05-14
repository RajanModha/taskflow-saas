using MediatR;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
