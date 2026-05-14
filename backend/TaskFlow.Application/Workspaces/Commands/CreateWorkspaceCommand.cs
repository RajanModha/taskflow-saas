using MediatR;

namespace TaskFlow.Application.Workspaces;

public sealed record CreateWorkspaceCommand(Guid UserId, CreateWorkspaceRequest Request) : IRequest<WorkspaceOutcome>;
