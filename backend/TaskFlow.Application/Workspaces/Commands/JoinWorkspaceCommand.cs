using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record JoinWorkspaceCommand(Guid UserId, JoinWorkspaceRequest Request) : IRequest<WorkspaceOutcome>;
