using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest<ForgotPasswordResponse>;
