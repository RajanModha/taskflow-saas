using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using Task = System.Threading.Tasks.Task;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class WorkspaceTaskTemplateRepository(TaskFlowDbContext dbContext) : IWorkspaceTaskTemplateRepository
{
    public async Task<IReadOnlyList<WorkspaceTaskTemplateReadModel>> ListTemplatesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var templates = await dbContext.TaskTemplates
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
        return await MapTemplatesAsync(templates, cancellationToken);
    }

    public async Task<WorkspaceTaskTemplateReadModel?> GetTemplateAsync(
        Guid organizationId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.TaskTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.OrganizationId == organizationId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        var mapped = await MapTemplatesAsync([template], cancellationToken);
        return mapped[0];
    }

    public async Task<bool> TemplateNameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludeTemplateId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.TaskTemplates.AsNoTracking()
            .Where(t => t.OrganizationId == organizationId && t.Name == name);
        if (excludeTemplateId.HasValue)
        {
            var excludeId = excludeTemplateId.Value;
            query = query.Where(t => t.Id != excludeId);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> CountValidTagsAsync(
        Guid organizationId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return 0;
        }

        return await dbContext.Tags.AsNoTracking()
            .CountAsync(t => t.OrganizationId == organizationId && tagIds.Contains(t.Id), cancellationToken);
    }

    public async Task<Guid> CreateTemplateAsync(
        Guid organizationId,
        Guid createdByUserId,
        DateTime nowUtc,
        WorkspaceTaskTemplateMutationInput input,
        CancellationToken cancellationToken)
    {
        var template = new TaskTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = input.Name,
            Description = input.Description,
            DefaultTitle = input.DefaultTitle,
            DefaultDescription = input.DefaultDescription,
            DefaultPriority = input.DefaultPriority,
            DefaultDueDaysFromNow = input.DefaultDueDaysFromNow,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.TaskTemplates.AddAsync(template, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await ReplaceTemplateChecklistAsync(template.Id, input.ChecklistItems, cancellationToken);
        await ReplaceTemplateTagsAsync(template.Id, input.TagIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return template.Id;
    }

    public async Task<bool> UpdateTemplateAsync(
        Guid organizationId,
        Guid templateId,
        DateTime nowUtc,
        WorkspaceTaskTemplateMutationInput input,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.TaskTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.OrganizationId == organizationId, cancellationToken);
        if (template is null)
        {
            return false;
        }

        template.Name = input.Name;
        template.Description = input.Description;
        template.DefaultTitle = input.DefaultTitle;
        template.DefaultDescription = input.DefaultDescription;
        template.DefaultPriority = input.DefaultPriority;
        template.DefaultDueDaysFromNow = input.DefaultDueDaysFromNow;
        template.UpdatedAtUtc = nowUtc;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await ReplaceTemplateChecklistAsync(template.Id, input.ChecklistItems, cancellationToken);
        await ReplaceTemplateTagsAsync(template.Id, input.TagIds, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteTemplateAsync(
        Guid organizationId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var deleted = await dbContext.TaskTemplates
            .Where(t => t.Id == templateId && t.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private async Task ReplaceTemplateChecklistAsync(
        Guid templateId,
        IReadOnlyList<string> checklistItems,
        CancellationToken cancellationToken)
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

    private async Task ReplaceTemplateTagsAsync(
        Guid templateId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        await dbContext.TaskTemplateTags
            .Where(tt => tt.TemplateId == templateId)
            .ExecuteDeleteAsync(cancellationToken);

        var tags = tagIds.Distinct()
            .Select(tagId => new TaskTemplateTag { TemplateId = templateId, TagId = tagId })
            .ToList();
        if (tags.Count > 0)
        {
            await dbContext.TaskTemplateTags.AddRangeAsync(tags, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<WorkspaceTaskTemplateReadModel>> MapTemplatesAsync(
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
                g => (IReadOnlyList<WorkspaceTemplateChecklistItemReadModel>)g
                    .OrderBy(i => i.Order)
                    .Select(i => new WorkspaceTemplateChecklistItemReadModel(i.Id, i.Title, i.Order))
                    .ToList());

        var tagsByTemplate = tagRows
            .GroupBy(x => x.TemplateId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<WorkspaceTemplateTagReadModel>)g
                    .Select(x => new WorkspaceTemplateTagReadModel(x.Tag.Id, x.Tag.Name, x.Tag.Color))
                    .OrderBy(t => t.Name)
                    .ToList());

        return templates.Select(t =>
            {
                creators.TryGetValue(t.CreatedByUserId, out var creator);
                checklistByTemplate.TryGetValue(t.Id, out var checklist);
                tagsByTemplate.TryGetValue(t.Id, out var tags);
                return new WorkspaceTaskTemplateReadModel(
                    t.Id,
                    t.Name,
                    t.Description,
                    t.DefaultTitle,
                    t.DefaultDescription,
                    t.DefaultPriority,
                    t.DefaultDueDaysFromNow,
                    t.CreatedByUserId,
                    creator?.UserName ?? string.Empty,
                    t.CreatedAtUtc,
                    checklist ?? [],
                    tags ?? []);
            })
            .ToList();
    }
}
