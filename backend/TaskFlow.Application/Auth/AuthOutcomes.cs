namespace TaskFlow.Application.Auth;

public abstract record RegisterOutcome;

public sealed record RegisterPendingEmailVerification(string Message) : RegisterOutcome;

public sealed record RegisterFailed(IReadOnlyDictionary<string, string[]> Errors) : RegisterOutcome;

public abstract record LoginOutcome;

public sealed record LoginSucceeded(AuthResponse Response) : LoginOutcome;

public sealed record LoginFailed(string Error) : LoginOutcome;

public sealed record LoginEmailNotVerified : LoginOutcome;

public abstract record VerifyEmailOutcome;

public sealed record VerifyEmailSucceeded(AuthResponse Response) : VerifyEmailOutcome;

public sealed record VerifyEmailFailed(string Title, string Detail, int StatusCode) : VerifyEmailOutcome;

public abstract record RefreshSessionOutcome;

public sealed record RefreshSessionSucceeded(AuthResponse Response) : RefreshSessionOutcome;

public sealed record RefreshSessionFailed(string Title, string Detail, int StatusCode) : RefreshSessionOutcome;

public sealed record RefreshSessionReuseDetected : RefreshSessionOutcome;

public abstract record ResetPasswordOutcome;

public sealed record ResetPasswordSucceeded(string Message) : ResetPasswordOutcome;

public sealed record ResetPasswordInvalidOrExpired : ResetPasswordOutcome;

public sealed record ResetPasswordSameAsCurrent : ResetPasswordOutcome;

/// <summary>Password does not satisfy Identity password policy (per-field messages).</summary>
public sealed record ResetPasswordPasswordPolicyFailed(IReadOnlyDictionary<string, string[]> Errors)
    : ResetPasswordOutcome;

/// <summary>Persistence failed after validation; client receives a generic message.</summary>
public sealed record ResetPasswordServerError : ResetPasswordOutcome;

public abstract record ChangePasswordOutcome;

public sealed record ChangePasswordSucceeded(string Message) : ChangePasswordOutcome;

public sealed record ChangePasswordWrongCurrentPassword : ChangePasswordOutcome;

public sealed record ChangePasswordNewSameAsCurrent : ChangePasswordOutcome;

public sealed record ChangePasswordInvalidRefresh : ChangePasswordOutcome;

public sealed record ChangePasswordPasswordPolicyFailed(IReadOnlyDictionary<string, string[]> Errors)
    : ChangePasswordOutcome;

public sealed record ChangePasswordServerError : ChangePasswordOutcome;

public abstract record UpdateProfileOutcome;

public sealed record UpdateProfileSucceeded(UserProfileResponse Response) : UpdateProfileOutcome;

public sealed record UpdateProfileUserNameConflict : UpdateProfileOutcome;

public sealed record UpdateProfileServerError : UpdateProfileOutcome;
