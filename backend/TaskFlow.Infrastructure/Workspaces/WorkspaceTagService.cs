using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Workspaces;

public sealed class WorkspaceTagService(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider) : IWorkspaceTagService
{
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<TagDto>?> ListTagsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var member = await LoadMemberInTenantAsync(userId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        return await dbContext.Tags
            .AsNoTracking()
            .Where(t => t.OrganizationId == member.OrganizationId)
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(cancellationToken);
    }

    public async Task<(int StatusCode, object? Body)> CreateTagAsync(
        Guid actorUserId,
        CreateWorkspaceTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        var name = request.Name.Trim();
        if (name.Length is 0 or > 30)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Name must be between 1 and 30 characters." });
        }

        if (!HexColorRegex.IsMatch(request.Color.Trim()))
        {
            return (StatusCodes.Status400BadRequest, new { message = "Color must be a hex value like #RRGGBB." });
        }

        var normalized = name.ToUpperInvariant();
        var exists = await dbContext.Tags
            .AnyAsync(t => t.OrganizationId == actor.OrganizationId && t.NormalizedName == normalized, cancellationToken);
        if (exists)
        {
            return (StatusCodes.Status409Conflict, new { message = "A tag with this name already exists in the workspace." });
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Name = name,
            NormalizedName = normalized,
            Color = request.Color.Trim(),
            CreatedAtUtc = now,
        };

        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (StatusCodes.Status201Created, new TagDto(tag.Id, tag.Name, tag.Color));
    }

    public async Task<(int StatusCode, object? Body)> UpdateTagAsync(
        Guid actorUserId,
        Guid tagId,
        UpdateWorkspaceTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        if (request.Name is null && request.Color is null)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Provide at least one of name or color." });
        }

        var tag = await dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.OrganizationId == actor.OrganizationId, cancellationToken);
        if (tag is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Tag not found." });
        }

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (name.Length is 0 or > 30)
            {
                return (StatusCodes.Status400BadRequest, new { message = "Name must be between 1 and 30 characters." });
            }

            var normalized = name.ToUpperInvariant();
            var nameTaken = await dbContext.Tags
                .AnyAsync(
                    t => t.OrganizationId == actor.OrganizationId &&
                         t.Id != tag.Id &&
                         t.NormalizedName == normalized,
                    cancellationToken);
            if (nameTaken)
            {
                return (StatusCodes.Status409Conflict, new { message = "A tag with this name already exists in the workspace." });
            }

            tag.Name = name;
            tag.NormalizedName = normalized;
        }

        if (request.Color is not null)
        {
            var color = request.Color.Trim();
            if (!HexColorRegex.IsMatch(color))
            {
                return (StatusCodes.Status400BadRequest, new { message = "Color must be a hex value like #RRGGBB." });
            }

            tag.Color = color;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return (StatusCodes.Status200OK, new TagDto(tag.Id, tag.Name, tag.Color));
    }

    public async Task<int> DeleteTagAsync(Guid actorUserId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var actor = await LoadAdminActorAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return StatusCodes.Status404NotFound;
        }

        var deleted = await dbContext.Tags
            .Where(t => t.Id == tagId && t.OrganizationId == actor.OrganizationId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0 ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
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
            .FirstOrDefaultAsync(
                u => u.Id == userId && u.OrganizationId == currentTenant.OrganizationId,
                cancellationToken);
    }

    private async Task<ApplicationUser?> LoadAdminActorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.OrganizationId == Guid.Empty)
        {
            return null;
        }

        if (user.OrganizationId != currentTenant.OrganizationId || !currentTenant.IsSet)
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
