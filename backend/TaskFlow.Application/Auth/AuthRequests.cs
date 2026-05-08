using MediatR;

namespace TaskFlow.Application.Auth;

public sealed record RegisterCommand(RegisterRequest Request) : IRequest<RegisterOutcome>;

public sealed record VerifyEmailCommand(VerifyEmailRequest Request) : IRequest<VerifyEmailOutcome>;

public sealed record ResendVerificationEmailCommand(ResendVerificationRequest Request) : IRequest;

public sealed record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest<ForgotPasswordResponse>;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<ResetPasswordOutcome>;

public sealed record RefreshSessionCommand(RefreshSessionRequest Request) : IRequest<RefreshSessionOutcome>;

public sealed record LogoutCommand(Guid UserId, LogoutRequest Request) : IRequest;

public sealed record LogoutAllCommand(Guid UserId) : IRequest;

public sealed record GetSessionsQuery(Guid UserId, string? RefreshTokenRawForCurrentMarker) : IRequest<IReadOnlyList<UserSessionItemDto>>;

public sealed record TryRevokeSessionCommand(Guid UserId, Guid SessionId) : IRequest<bool>;

public sealed record LoginCommand(LoginRequest Request) : IRequest<LoginOutcome>;

public sealed record GetProfileQuery(Guid UserId) : IRequest<UserProfileResponse?>;

public sealed record ChangePasswordCommand(Guid UserId, ChangePasswordRequest Request) : IRequest<ChangePasswordOutcome>;

public sealed record UpdateProfileCommand(Guid UserId, UpdateProfileRequest Request) : IRequest<UpdateProfileOutcome>;
