using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record VerifyEmailCommand(VerifyEmailRequest Request) : IRequest<VerifyEmailOutcome>;
