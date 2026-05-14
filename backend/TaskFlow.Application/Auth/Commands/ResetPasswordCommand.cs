using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<ResetPasswordOutcome>;
