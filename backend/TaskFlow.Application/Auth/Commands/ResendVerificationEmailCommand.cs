using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record ResendVerificationEmailCommand(ResendVerificationRequest Request) : IRequest;
