namespace TaskFlow.Domain.Repositories;

public interface IWorkspaceTaskTemplateRepository
{
    Task<IReadOnlyList<WorkspaceTaskTemplateReadModel>> ListTemplatesAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<WorkspaceTaskTemplateReadModel?> GetTemplateAsync(
        Guid organizationId,
        Guid templateId,
        CancellationToken cancellationToken);

    Task<bool> TemplateNameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludeTemplateId,
        CancellationToken cancellationToken);

    Task<int> CountValidTagsAsync(
        Guid organizationId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken);

    Task<Guid> CreateTemplateAsync(
        Guid organizationId,
        Guid createdByUserId,
        DateTime nowUtc,
        WorkspaceTaskTemplateMutationInput input,
        CancellationToken cancellationToken);

    Task<bool> UpdateTemplateAsync(
        Guid organizationId,
        Guid templateId,
        DateTime nowUtc,
        WorkspaceTaskTemplateMutationInput input,
        CancellationToken cancellationToken);

    Task<bool> DeleteTemplateAsync(
        Guid organizationId,
        Guid templateId,
        CancellationToken cancellationToken);
}
