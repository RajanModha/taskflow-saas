namespace TaskFlow.Application.Workspaces;

public abstract record WorkspaceMembersPageOutcome;

public sealed record WorkspaceMembersPageOk(WorkspaceMembersPageResponse Value) : WorkspaceMembersPageOutcome;

public sealed record WorkspaceMembersPageNotFoundOutcome : WorkspaceMembersPageOutcome;

public sealed record WorkspaceMembersPageBadRequestOutcome(string Message) : WorkspaceMembersPageOutcome;
