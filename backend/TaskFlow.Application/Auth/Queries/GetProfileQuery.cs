using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record GetProfileQuery(Guid UserId) : IRequest<UserProfileResponse?>;
