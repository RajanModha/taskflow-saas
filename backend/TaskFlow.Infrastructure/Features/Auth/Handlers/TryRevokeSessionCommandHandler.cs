using MediatR;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
