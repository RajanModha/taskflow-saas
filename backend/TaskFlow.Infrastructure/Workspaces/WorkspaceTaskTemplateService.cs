using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using Task = System.Threading.Tasks.Task;

namespace TaskFlow.Infrastructure.Workspaces;

public sealed class WorkspaceTaskTemplateService(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider) : IWorkspaceTaskTemplateService
{
    public async Task<IReadOnlyList<TaskTemplateDto>?> ListTemplatesAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await LoadMemberInTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var templates = await dbContext.TaskTemplates
            .AsNoTracking()
            .Where(t => t.OrganizationId == actor.OrganizationId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return await MapTemplatesAsync(templates, cancellationToken);
    }

    public async Task<TaskTemplateDto?> GetTemplateAsync(Guid actorUserId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var actor = await LoadMemberInTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var template = await dbContext.TaskTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.OrganizationId == actor.OrganizationId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        var mapped = await MapTemplatesAsync([template], cancellationToken);
        return mapped[0];
    }

    public async Task<(int StatusCode, object? Body)> CreateTemplateAsync(
        Guid actorUserId,
        CreateTaskTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
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

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var template = new TaskTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DefaultTitle = request.DefaultTitle.Trim(),
            DefaultDescription = string.IsNullOrWhiteSpace(request.DefaultDescription) ? null : request.DefaultDescription.Trim(),
            DefaultPriority = request.DefaultPriority,
            DefaultDueDaysFromNow = request.DefaultDueDaysFromNow,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        await using var createTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.TaskTemplates.AddAsync(template, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await ReplaceTemplateChecklistAsync(template.Id, request.ChecklistItems, cancellationToken);
        await ReplaceTemplateTagsAsync(template.Id, request.TagIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await createTransaction.CommitAsync(cancellationToken);

        var mapped = await MapTemplatesAsync([template], cancellationToken);
        return (StatusCodes.Status201Created, mapped[0]);
    }

    public async Task<(int StatusCode, object? Body)> UpdateTemplateAsync(
        Guid actorUserId,
        Guid templateId,
        UpdateTaskTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        var template = await dbContext.TaskTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.OrganizationId == actor.OrganizationId, cancellationToken);
        if (template is null)
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

        template.Name = request.Name.Trim();
        template.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        template.DefaultTitle = request.DefaultTitle.Trim();
        template.DefaultDescription = string.IsNullOrWhiteSpace(request.DefaultDescription) ? null : request.DefaultDescription.Trim();
        template.DefaultPriority = request.DefaultPriority;
        template.DefaultDueDaysFromNow = request.DefaultDueDaysFromNow;
        template.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var updateTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await ReplaceTemplateChecklistAsync(template.Id, request.ChecklistItems, cancellationToken);
        await ReplaceTemplateTagsAsync(template.Id, request.TagIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await updateTransaction.CommitAsync(cancellationToken);

        var mapped = await MapTemplatesAsync([template], cancellationToken);
        return (StatusCodes.Status200OK, mapped[0]);
    }

    public async Task<int> DeleteTemplateAsync(Guid actorUserId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return StatusCodes.Status404NotFound;
        }

        var deleted = await dbContext.TaskTemplates
            .Where(t => t.Id == templateId && t.OrganizationId == actor.OrganizationId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0 ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
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

        var nameTaken = await dbContext.TaskTemplates
            .AnyAsync(
                t => t.OrganizationId == organizationId &&
                     t.Name == trimmedName &&
                     (!editingTemplateId.HasValue || t.Id != editingTemplateId.Value),
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
            var validCount = await dbContext.Tags
                .AsNoTracking()
                .CountAsync(t => t.OrganizationId == organizationId && uniqueTagIds.Contains(t.Id), cancellationToken);
            if (validCount != uniqueTagIds.Length)
            {
                return (StatusCodes.Status400BadRequest, new { message = "One or more tag ids are invalid for this workspace." });
            }
        }

        return null;
    }

    private async Task ReplaceTemplateChecklistAsync(Guid templateId, IReadOnlyList<string> checklistItems, CancellationToken cancellationToken)
    {
        await dbContext.TaskTemplateChecklistItems
            .Where(i => i.TemplateId == templateId)
            .ExecuteDeleteAsync(cancellationToken);

        var items = checklistItems
            .Select((title, index) => new TaskTemplateChecklistItem
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                Title = title.Trim(),
                Order = index + 1,
            })
            .ToList();

        if (items.Count > 0)
        {
            await dbContext.TaskTemplateChecklistItems.AddRangeAsync(items, cancellationToken);
        }
    }

    private async Task ReplaceTemplateTagsAsync(Guid templateId, IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken)
    {
        await dbContext.TaskTemplateTags
            .Where(tt => tt.TemplateId == templateId)
            .ExecuteDeleteAsync(cancellationToken);

        var tags = tagIds
            .Distinct()
            .Select(tagId => new TaskTemplateTag { TemplateId = templateId, TagId = tagId })
            .ToList();

        if (tags.Count > 0)
        {
            await dbContext.TaskTemplateTags.AddRangeAsync(tags, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<TaskTemplateDto>> MapTemplatesAsync(
        IReadOnlyList<TaskTemplate> templates,
        CancellationToken cancellationToken)
    {
        if (templates.Count == 0)
        {
            return [];
        }

        var templateIds = templates.Select(t => t.Id).ToArray();
        var creatorIds = templates.Select(t => t.CreatedByUserId).Distinct().ToArray();

        var creators = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => creatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var checklistRows = await dbContext.TaskTemplateChecklistItems
            .AsNoTracking()
            .Where(i => templateIds.Contains(i.TemplateId))
            .OrderBy(i => i.Order)
            .ToListAsync(cancellationToken);

        var tagRows = await dbContext.TaskTemplateTags
            .AsNoTracking()
            .Where(tt => templateIds.Contains(tt.TemplateId))
            .Join(
                dbContext.Tags.AsNoTracking(),
                tt => tt.TagId,
                tag => tag.Id,
                (tt, tag) => new { tt.TemplateId, Tag = tag })
            .ToListAsync(cancellationToken);

        var checklistByTemplate = checklistRows
            .GroupBy(x => x.TemplateId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<TaskTemplateChecklistItemDto>)g
                    .OrderBy(i => i.Order)
                    .Select(i => new TaskTemplateChecklistItemDto(i.Id, i.Title, i.Order))
                    .ToList());

        var tagsByTemplate = tagRows
            .GroupBy(x => x.TemplateId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<TagDto>)g
                    .Select(x => new TagDto(x.Tag.Id, x.Tag.Name, x.Tag.Color))
                    .OrderBy(t => t.Name)
                    .ToList());

        return templates.Select(t =>
            {
                creators.TryGetValue(t.CreatedByUserId, out var creator);
                var createdBy = new TaskTemplateCreatedByDto(
                    t.CreatedByUserId,
                    creator?.UserName ?? string.Empty);
                checklistByTemplate.TryGetValue(t.Id, out var checklist);
                tagsByTemplate.TryGetValue(t.Id, out var tags);
                return new TaskTemplateDto(
                    t.Id,
                    t.Name,
                    t.Description,
                    t.DefaultTitle,
                    t.DefaultDescription,
                    t.DefaultPriority,
                    t.DefaultDueDaysFromNow,
                    checklist ?? [],
                    tags ?? [],
                    createdBy,
                    t.CreatedAtUtc);
            })
            .ToList();
    }

    private async Task<ApplicationUser?> LoadMemberInTenantAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            return null;
        }

        return await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == currentTenant.OrganizationId, cancellationToken);
    }

    private async Task<ApplicationUser?> LoadAdminActorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.OrganizationId == Guid.Empty)
        {
            return null;
        }

        if (!currentTenant.IsSet || user.OrganizationId != currentTenant.OrganizationId)
        {
            return null;
        }

        if (user.WorkspaceRole != WorkspaceRole.Owner && user.WorkspaceRole != WorkspaceRole.Admin)
        {
            return null;
        }

        return user;
    }
}
