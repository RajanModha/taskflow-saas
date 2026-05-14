using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record GetSessionsQuery(Guid UserId, string? RefreshTokenRawForCurrentMarker) : IRequest<IReadOnlyList<UserSessionItemDto>>;
