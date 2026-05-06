using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Workspaces;

public sealed class WorkspaceTagService(
    IWorkspaceAccessRepository workspaceAccessRepository,
    IWorkspaceTagRepository workspaceTagRepository,
    TimeProvider timeProvider) : IWorkspaceTagService
{
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<TagDto>?> ListTagsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(userId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var tags = await workspaceTagRepository.ListTagsAsync(actor.OrganizationId, cancellationToken);
        return tags.Select(t => new TagDto(t.Id, t.Name, t.Color)).ToList();
    }

    public async Task<(int StatusCode, object? Body)> CreateTagAsync(
        Guid actorUserId,
        CreateWorkspaceTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(actorUserId, cancellationToken);
        if (actor is null || (actor.WorkspaceRole != WorkspaceRole.Owner && actor.WorkspaceRole != WorkspaceRole.Admin))
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
        var exists = await workspaceTagRepository.TagNameExistsAsync(actor.OrganizationId, normalized, null, cancellationToken);
        if (exists)
        {
            return (StatusCodes.Status409Conflict, new { message = "A tag with this name already exists in the workspace." });
        }

        var created = await workspaceTagRepository.CreateTagAsync(
            actor.OrganizationId,
            name,
            normalized,
            request.Color.Trim(),
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
        return (StatusCodes.Status201Created, new TagDto(created.Id, created.Name, created.Color));
    }

    public async Task<(int StatusCode, object? Body)> UpdateTagAsync(
        Guid actorUserId,
        Guid tagId,
        UpdateWorkspaceTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(actorUserId, cancellationToken);
        if (actor is null || (actor.WorkspaceRole != WorkspaceRole.Owner && actor.WorkspaceRole != WorkspaceRole.Admin))
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        if (request.Name is null && request.Color is null)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Provide at least one of name or color." });
        }

        string? updatedName = null;
        string? updatedNormalizedName = null;
        string? updatedColor = null;

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (name.Length is 0 or > 30)
            {
                return (StatusCodes.Status400BadRequest, new { message = "Name must be between 1 and 30 characters." });
            }

            var normalized = name.ToUpperInvariant();
            var nameTaken = await workspaceTagRepository.TagNameExistsAsync(
                actor.OrganizationId,
                normalized,
                tagId,
                cancellationToken);
            if (nameTaken)
            {
                return (StatusCodes.Status409Conflict, new { message = "A tag with this name already exists in the workspace." });
            }

            updatedName = name;
            updatedNormalizedName = normalized;
        }

        if (request.Color is not null)
        {
            var color = request.Color.Trim();
            if (!HexColorRegex.IsMatch(color))
            {
                return (StatusCodes.Status400BadRequest, new { message = "Color must be a hex value like #RRGGBB." });
            }

            updatedColor = color;
        }

        var updated = await workspaceTagRepository.UpdateTagAsync(
            actor.OrganizationId,
            tagId,
            updatedName,
            updatedNormalizedName,
            updatedColor,
            cancellationToken);
        if (updated is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Tag not found." });
        }
        return (StatusCodes.Status200OK, new TagDto(updated.Id, updated.Name, updated.Color));
    }

    public async Task<int> DeleteTagAsync(Guid actorUserId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var actor = await workspaceAccessRepository.GetActorInCurrentTenantAsync(actorUserId, cancellationToken);
        if (actor is null || (actor.WorkspaceRole != WorkspaceRole.Owner && actor.WorkspaceRole != WorkspaceRole.Admin))
        {
            return StatusCodes.Status404NotFound;
        }

        var deleted = await workspaceTagRepository.DeleteTagAsync(actor.OrganizationId, tagId, cancellationToken);
        return deleted ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
    }
}
