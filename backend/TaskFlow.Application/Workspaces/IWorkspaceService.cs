namespace TaskFlow.Application.Workspaces;

public interface IWorkspaceService
{
    Task<WorkspaceOutcome> CreateAsync(Guid userId, CreateWorkspaceRequest request, CancellationToken cancellationToken = default);
    Task<WorkspaceOutcome> JoinAsync(Guid userId, JoinWorkspaceRequest request, CancellationToken cancellationToken = default);
}

