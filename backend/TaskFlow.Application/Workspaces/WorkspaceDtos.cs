namespace TaskFlow.Application.Workspaces;

public sealed record CreateWorkspaceRequest(string Name);

public sealed record JoinWorkspaceRequest(string Code);

