using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class WorkspaceTagRepository(TaskFlowDbContext dbContext) : IWorkspaceTagRepository
{
    public async Task<IReadOnlyList<WorkspaceTagReadModel>> ListTagsAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Tags
            .AsNoTracking()
            .Where(t => t.OrganizationId == organizationId)
            .OrderBy(t => t.Name)
            .Select(t => new WorkspaceTagReadModel(t.Id, t.Name, t.Color))
            .ToListAsync(cancellationToken);

    public async Task<bool> TagNameExistsAsync(
        Guid organizationId,
        string normalizedName,
        Guid? excludeTagId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Tags.AsNoTracking()
            .Where(t => t.OrganizationId == organizationId && t.NormalizedName == normalizedName);
        if (excludeTagId.HasValue)
        {
            var excludeId = excludeTagId.Value;
            query = query.Where(t => t.Id != excludeId);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<WorkspaceTagReadModel> CreateTagAsync(
        Guid organizationId,
        string name,
        string normalizedName,
        string color,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            NormalizedName = normalizedName,
            Color = color,
            CreatedAtUtc = createdAtUtc,
        };

        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new WorkspaceTagReadModel(tag.Id, tag.Name, tag.Color);
    }

    public async Task<WorkspaceTagReadModel?> UpdateTagAsync(
        Guid organizationId,
        Guid tagId,
        string? name,
        string? normalizedName,
        string? color,
        CancellationToken cancellationToken)
    {
        var tag = await dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.OrganizationId == organizationId, cancellationToken);
        if (tag is null)
        {
            return null;
        }

        if (name is not null)
        {
            tag.Name = name;
            tag.NormalizedName = normalizedName ?? tag.NormalizedName;
        }

        if (color is not null)
        {
            tag.Color = color;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new WorkspaceTagReadModel(tag.Id, tag.Name, tag.Color);
    }

    public async Task<bool> DeleteTagAsync(
        Guid organizationId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        var deleted = await dbContext.Tags
            .Where(t => t.Id == tagId && t.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
