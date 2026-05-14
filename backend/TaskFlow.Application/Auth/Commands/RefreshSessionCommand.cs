using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record RefreshSessionCommand(RefreshSessionRequest Request) : IRequest<RefreshSessionOutcome>;
