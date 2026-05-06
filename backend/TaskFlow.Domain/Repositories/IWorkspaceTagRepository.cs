namespace TaskFlow.Domain.Repositories;

public interface IWorkspaceTagRepository
{
    Task<IReadOnlyList<WorkspaceTagReadModel>> ListTagsAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<bool> TagNameExistsAsync(
        Guid organizationId,
        string normalizedName,
        Guid? excludeTagId,
        CancellationToken cancellationToken);

    Task<WorkspaceTagReadModel> CreateTagAsync(
        Guid organizationId,
        string name,
        string normalizedName,
        string color,
        DateTime createdAtUtc,
        CancellationToken cancellationToken);

    Task<WorkspaceTagReadModel?> UpdateTagAsync(
        Guid organizationId,
        Guid tagId,
        string? name,
        string? normalizedName,
        string? color,
        CancellationToken cancellationToken);

    Task<bool> DeleteTagAsync(
        Guid organizationId,
        Guid tagId,
        CancellationToken cancellationToken);
}
