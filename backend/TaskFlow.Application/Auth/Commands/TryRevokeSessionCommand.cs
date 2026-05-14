using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record TryRevokeSessionCommand(Guid UserId, Guid SessionId) : IRequest<bool>;
