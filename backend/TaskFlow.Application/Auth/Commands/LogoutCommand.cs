using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record LogoutCommand(Guid UserId, LogoutRequest Request) : IRequest;
