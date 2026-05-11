using MediatR;
using TaskFlow.Application.Auth;

namespace TaskFlow.Infrastructure.Auth;

public sealed class AuthTimingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is ResetPasswordCommand or RefreshSessionCommand or ChangePasswordCommand)
        {
            await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
        }

        return await next();
    }
}
