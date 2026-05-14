using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record RegisterCommand(RegisterRequest Request) : IRequest<RegisterOutcome>;
