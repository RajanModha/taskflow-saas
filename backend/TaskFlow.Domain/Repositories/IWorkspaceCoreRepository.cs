namespace TaskFlow.Domain.Repositories;

public interface IWorkspaceCoreRepository
{
    Task<bool> JoinCodeExistsAsync(
        string joinCode,
        CancellationToken cancellationToken);

    Task<Guid> CreateOrganizationAsync(
        string name,
        string joinCode,
        DateTime createdAtUtc,
        CancellationToken cancellationToken);

    Task<WorkspaceOrganizationReadModel?> GetOrganizationByJoinCodeAsync(
        string joinCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkspaceAdminReadModel>> GetOrganizationAdminsAsync(
        Guid organizationId,
        CancellationToken cancellationToken);
}
