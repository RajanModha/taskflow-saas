using TaskFlow.Application.Auth;

namespace TaskFlow.Application.Workspaces;

public abstract record WorkspaceOutcome;

public sealed record WorkspaceSucceeded(AuthResponse Response) : WorkspaceOutcome;

public sealed record WorkspaceFailed(IReadOnlyDictionary<string, string[]> Errors) : WorkspaceOutcome;

