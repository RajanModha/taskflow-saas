using Microsoft.AspNetCore.Http;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using Task = System.Threading.Tasks.Task;

namespace TaskFlow.Infrastructure.Workspaces;

public sealed class WorkspaceTaskTemplateService(
    IWorkspaceAccessRepository workspaceAccessRepository,
    IWorkspaceTaskTemplateRepository workspaceTaskTemplateRepository,
    TimeProvider timeProvider) : IWorkspaceTaskTemplateService
{
    public async Task<IReadOnlyList<TaskTemplateDto>?> ListTemplatesAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var templates = await workspaceTaskTemplateRepository.ListTemplatesAsync(actor.OrganizationId, cancellationToken);
        return templates.Select(ToDto).ToList();
    }

    public async Task<TaskTemplateDto?> GetTemplateAsync(Guid actorUserId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var template = await workspaceTaskTemplateRepository.GetTemplateAsync(actor.OrganizationId, templateId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        return ToDto(template);
    }

    public async Task<(int StatusCode, object? Body)> CreateTemplateAsync(
        Guid actorUserId,
        CreateTaskTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(actorUserId, cancellationToken);
        if (actor is null || (actor.WorkspaceRole != WorkspaceRole.Owner && actor.WorkspaceRole != WorkspaceRole.Admin))
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        var validation = await ValidateCreateOrUpdateAsync(
            actor.OrganizationId,
            null,
            request.Name,
            request.Description,
            request.DefaultTitle,
            request.DefaultDescription,
            request.DefaultDueDaysFromNow,
            request.ChecklistItems,
            request.TagIds,
            cancellationToken);
        if (validation is not null)
        {
            return validation.Value;
        }

        var createdId = await workspaceTaskTemplateRepository.CreateTemplateAsync(
            actor.OrganizationId,
            actorUserId,
            timeProvider.GetUtcNow().UtcDateTime,
            new WorkspaceTaskTemplateMutationInput(
                request.Name.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                request.DefaultTitle.Trim(),
                string.IsNullOrWhiteSpace(request.DefaultDescription) ? null : request.DefaultDescription.Trim(),
                request.DefaultPriority,
                request.DefaultDueDaysFromNow,
                request.ChecklistItems,
                request.TagIds),
            cancellationToken);
        var created = await workspaceTaskTemplateRepository.GetTemplateAsync(actor.OrganizationId, createdId, cancellationToken);
        return (StatusCodes.Status201Created, created is null ? null : ToDto(created));
    }

    public async Task<(int StatusCode, object? Body)> UpdateTemplateAsync(
        Guid actorUserId,
        Guid templateId,
        UpdateTaskTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(actorUserId, cancellationToken);
        if (actor is null || (actor.WorkspaceRole != WorkspaceRole.Owner && actor.WorkspaceRole != WorkspaceRole.Admin))
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        var existing = await workspaceTaskTemplateRepository.GetTemplateAsync(actor.OrganizationId, templateId, cancellationToken);
        if (existing is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Template not found." });
        }

        var validation = await ValidateCreateOrUpdateAsync(
            actor.OrganizationId,
            templateId,
            request.Name,
            request.Description,
            request.DefaultTitle,
            request.DefaultDescription,
            request.DefaultDueDaysFromNow,
            request.ChecklistItems,
            request.TagIds,
            cancellationToken);
        if (validation is not null)
        {
            return validation.Value;
        }

        var updated = await workspaceTaskTemplateRepository.UpdateTemplateAsync(
            actor.OrganizationId,
            templateId,
            timeProvider.GetUtcNow().UtcDateTime,
            new WorkspaceTaskTemplateMutationInput(
                request.Name.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                request.DefaultTitle.Trim(),
                string.IsNullOrWhiteSpace(request.DefaultDescription) ? null : request.DefaultDescription.Trim(),
                request.DefaultPriority,
                request.DefaultDueDaysFromNow,
                request.ChecklistItems,
                request.TagIds),
            cancellationToken);
        if (!updated)
        {
            return (StatusCodes.Status404NotFound, new { message = "Template not found." });
        }

        var reloaded = await workspaceTaskTemplateRepository.GetTemplateAsync(actor.OrganizationId, templateId, cancellationToken);
        return (StatusCodes.Status200OK, reloaded is null ? null : ToDto(reloaded));
    }

    public async Task<int> DeleteTemplateAsync(Guid actorUserId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(actorUserId, cancellationToken);
        if (actor is null || (actor.WorkspaceRole != WorkspaceRole.Owner && actor.WorkspaceRole != WorkspaceRole.Admin))
        {
            return StatusCodes.Status404NotFound;
        }

        var deleted = await workspaceTaskTemplateRepository.DeleteTemplateAsync(actor.OrganizationId, templateId, cancellationToken);
        return deleted ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
    }

    private async Task<(int StatusCode, object? Body)?> ValidateCreateOrUpdateAsync(
        Guid organizationId,
        Guid? editingTemplateId,
        string name,
        string? description,
        string defaultTitle,
        string? defaultDescription,
        int? defaultDueDaysFromNow,
        IReadOnlyList<string> checklistItems,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();
        if (trimmedName.Length is 0 or > 100)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Name must be between 1 and 100 characters." });
        }

        if (description is not null && description.Trim().Length > 500)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Description cannot exceed 500 characters." });
        }

        var trimmedTitle = defaultTitle.Trim();
        if (trimmedTitle.Length is 0 or > 200)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Default title must be between 1 and 200 characters." });
        }

        if (defaultDescription is not null && defaultDescription.Trim().Length > 4000)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Default description cannot exceed 4000 characters." });
        }

        if (defaultDueDaysFromNow is < 0 or > 3650)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Default due days from now must be between 0 and 3650." });
        }

        var nameTaken = await workspaceTaskTemplateRepository.TemplateNameExistsAsync(
            organizationId,
            trimmedName,
            editingTemplateId,
            cancellationToken);
        if (nameTaken)
        {
            return (StatusCodes.Status409Conflict, new { message = "A template with this name already exists in the workspace." });
        }

        if (checklistItems.Count > 20)
        {
            return (StatusCodes.Status400BadRequest, new { message = "At most 20 checklist items are allowed." });
        }

        if (tagIds.Count > 10)
        {
            return (StatusCodes.Status400BadRequest, new { message = "At most 10 tags are allowed." });
        }

        if (checklistItems.Any(i => string.IsNullOrWhiteSpace(i) || i.Trim().Length > 200))
        {
            return (StatusCodes.Status400BadRequest, new { message = "Each checklist item must be between 1 and 200 characters." });
        }

        var uniqueTagIds = tagIds.Distinct().ToArray();
        if (uniqueTagIds.Length != tagIds.Count)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Duplicate tag ids are not allowed." });
        }

        if (uniqueTagIds.Length > 0)
        {
            var validCount = await workspaceTaskTemplateRepository.CountValidTagsAsync(
                organizationId,
                uniqueTagIds,
                cancellationToken);
            if (validCount != uniqueTagIds.Length)
            {
                return (StatusCodes.Status400BadRequest, new { message = "One or more tag ids are invalid for this workspace." });
            }
        }

        return null;
    }

    private static TaskTemplateDto ToDto(WorkspaceTaskTemplateReadModel t) =>
        new(
            t.Id,
            t.Name,
            t.Description,
            t.DefaultTitle,
            t.DefaultDescription,
            t.DefaultPriority,
            t.DefaultDueDaysFromNow,
            t.ChecklistItems.Select(i => new TaskTemplateChecklistItemDto(i.Id, i.Title, i.Order)).ToList(),
            t.Tags.Select(tag => new TagDto(tag.Id, tag.Name, tag.Color)).ToList(),
            new TaskTemplateCreatedByDto(t.CreatedByUserId, t.CreatedByUserName),
            t.CreatedAtUtc);
}
