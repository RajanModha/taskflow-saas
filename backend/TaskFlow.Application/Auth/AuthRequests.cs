using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record RegisterCommand(RegisterRequest Request) : IRequest<RegisterOutcome>;

public sealed class RegisterCommandHandler(IAuthService authService) : IRequestHandler<RegisterCommand, RegisterOutcome>
{
    public Task<RegisterOutcome> Handle(RegisterCommand request, CancellationToken cancellationToken)
        => authService.RegisterAsync(request.Request, cancellationToken);
}

public sealed record VerifyEmailCommand(VerifyEmailRequest Request) : IRequest<VerifyEmailOutcome>;

public sealed class VerifyEmailCommandHandler(IAuthService authService) : IRequestHandler<VerifyEmailCommand, VerifyEmailOutcome>
{
    public Task<VerifyEmailOutcome> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        => authService.VerifyEmailAsync(request.Request, cancellationToken);
}

public sealed record ResendVerificationEmailCommand(ResendVerificationRequest Request) : IRequest;

public sealed class ResendVerificationEmailCommandHandler(IAuthService authService) : IRequestHandler<ResendVerificationEmailCommand>
{
    public async Task<Unit> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        await authService.ResendVerificationEmailAsync(request.Request, cancellationToken);
        return Unit.Value;
    }
}

public sealed record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest<ForgotPasswordResponse>;

public sealed class ForgotPasswordCommandHandler(IAuthService authService) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    public Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        => authService.ForgotPasswordAsync(request.Request, cancellationToken);
}

public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<ResetPasswordOutcome>;

public sealed class ResetPasswordCommandHandler(IAuthService authService) : IRequestHandler<ResetPasswordCommand, ResetPasswordOutcome>
{
    public Task<ResetPasswordOutcome> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        => authService.ResetPasswordAsync(request.Request, cancellationToken);
}

public sealed record RefreshSessionCommand(RefreshSessionRequest Request) : IRequest<RefreshSessionOutcome>;

public sealed class RefreshSessionCommandHandler(IAuthService authService) : IRequestHandler<RefreshSessionCommand, RefreshSessionOutcome>
{
    public Task<RefreshSessionOutcome> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
        => authService.RefreshSessionAsync(request.Request, cancellationToken);
}

public sealed record LogoutCommand(Guid UserId, LogoutRequest Request) : IRequest;

public sealed class LogoutCommandHandler(IAuthService authService) : IRequestHandler<LogoutCommand>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request.UserId, request.Request, cancellationToken);
        return Unit.Value;
    }
}

public sealed record LogoutAllCommand(Guid UserId) : IRequest;

public sealed class LogoutAllCommandHandler(IAuthService authService) : IRequestHandler<LogoutAllCommand>
{
    public async Task<Unit> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
    {
        await authService.LogoutAllAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}

public sealed record GetSessionsQuery(Guid UserId, string? RefreshTokenRawForCurrentMarker) : IRequest<IReadOnlyList<UserSessionItemDto>>;

public sealed class GetSessionsQueryHandler(IAuthService authService) : IRequestHandler<GetSessionsQuery, IReadOnlyList<UserSessionItemDto>>
{
    public Task<IReadOnlyList<UserSessionItemDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
        => authService.GetSessionsAsync(request.UserId, request.RefreshTokenRawForCurrentMarker, cancellationToken);
}

public sealed record TryRevokeSessionCommand(Guid UserId, Guid SessionId) : IRequest<bool>;

public sealed class TryRevokeSessionCommandHandler(IAuthService authService) : IRequestHandler<TryRevokeSessionCommand, bool>
{
    public Task<bool> Handle(TryRevokeSessionCommand request, CancellationToken cancellationToken)
        => authService.TryRevokeSessionAsync(request.UserId, request.SessionId, cancellationToken);
}

public sealed record LoginCommand(LoginRequest Request) : IRequest<LoginOutcome>;

public sealed class LoginCommandHandler(IAuthService authService) : IRequestHandler<LoginCommand, LoginOutcome>
{
    public Task<LoginOutcome> Handle(LoginCommand request, CancellationToken cancellationToken)
        => authService.LoginAsync(request.Request, cancellationToken);
}

public sealed record GetProfileQuery(Guid UserId) : IRequest<UserProfileResponse?>;

public sealed class GetProfileQueryHandler(IAuthService authService) : IRequestHandler<GetProfileQuery, UserProfileResponse?>
{
    public Task<UserProfileResponse?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        => authService.GetProfileAsync(request.UserId, cancellationToken);
}

public sealed record ChangePasswordCommand(Guid UserId, ChangePasswordRequest Request) : IRequest<ChangePasswordOutcome>;

public sealed class ChangePasswordCommandHandler(IAuthService authService) : IRequestHandler<ChangePasswordCommand, ChangePasswordOutcome>
{
    public Task<ChangePasswordOutcome> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        => authService.ChangePasswordAsync(request.UserId, request.Request, cancellationToken);
}

public sealed record UpdateProfileCommand(Guid UserId, UpdateProfileRequest Request) : IRequest<UpdateProfileOutcome>;

public sealed class UpdateProfileCommandHandler(IAuthService authService) : IRequestHandler<UpdateProfileCommand, UpdateProfileOutcome>
{
    public Task<UpdateProfileOutcome> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        => authService.UpdateProfileAsync(request.UserId, request.Request, cancellationToken);
}
