using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record LoginCommand(LoginRequest Request) : IRequest<LoginOutcome>;
