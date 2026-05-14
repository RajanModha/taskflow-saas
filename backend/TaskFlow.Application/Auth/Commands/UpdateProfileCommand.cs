using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record UpdateProfileCommand(Guid UserId, UpdateProfileRequest Request) : IRequest<UpdateProfileOutcome>;
