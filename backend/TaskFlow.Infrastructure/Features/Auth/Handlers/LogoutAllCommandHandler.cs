using MediatR;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

public sealed class LogoutAllCommandHandler(IAuthRepository authRepository, TimeProvider timeProvider) : IRequestHandler<LogoutAllCommand>
{
    public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await authRepository.RevokeAllActiveRefreshTokensForUserAsync(request.UserId, now, cancellationToken);
        return Unit.Value;
    }
}
