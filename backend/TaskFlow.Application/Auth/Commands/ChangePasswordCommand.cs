using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record ChangePasswordCommand(Guid UserId, ChangePasswordRequest Request) : IRequest<ChangePasswordOutcome>;
