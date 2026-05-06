namespace TaskFlow.Domain.Repositories;

public interface IWorkspaceAccessRepository
{
    Task<WorkspaceActorContext?> GetActorInCurrentTenantAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
