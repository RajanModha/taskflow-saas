namespace TaskFlow.Application.Auth;

public abstract record RegisterOutcome;

public sealed record RegisterSucceeded(AuthResponse Response) : RegisterOutcome;

public sealed record RegisterFailed(IReadOnlyDictionary<string, string[]> Errors) : RegisterOutcome;

public abstract record LoginOutcome;

public sealed record LoginSucceeded(AuthResponse Response) : LoginOutcome;

public sealed record LoginFailed(string Error) : LoginOutcome;
