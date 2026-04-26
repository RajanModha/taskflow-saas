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

public abstract record ResetPasswordOutcome;

public sealed record ResetPasswordSucceeded(string Message) : ResetPasswordOutcome;

public sealed record ResetPasswordInvalidOrExpired : ResetPasswordOutcome;

public sealed record ResetPasswordSameAsCurrent : ResetPasswordOutcome;

/// <summary>Password does not satisfy Identity password policy (per-field messages).</summary>
public sealed record ResetPasswordPasswordPolicyFailed(IReadOnlyDictionary<string, string[]> Errors)
    : ResetPasswordOutcome;

/// <summary>Persistence failed after validation; client receives a generic message.</summary>
public sealed record ResetPasswordServerError : ResetPasswordOutcome;
