using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record LogoutAllCommand(Guid UserId) : IRequest;
