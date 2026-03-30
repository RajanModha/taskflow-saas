using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Auth;

namespace TaskFlow.Infrastructure.Workspaces;

public sealed class WorkspaceService(
    UserManager<ApplicationUser> userManager,
    TaskFlowDbContext dbContext,
    IJwtTokenGenerator tokenGenerator,
    TimeProvider timeProvider) : IWorkspaceService
{
    public async Task<WorkspaceOutcome> CreateAsync(
        Guid userId,
        CreateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new WorkspaceFailed(new Dictionary<string, string[]>
            {
                { "general", [ "User not found." ] }
            });
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var joinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            JoinCode = joinCode,
            CreatedAtUtc = now,
        };

        await dbContext.Organizations.AddAsync(organization, cancellationToken);

        user.OrganizationId = organization.Id;
        await userManager.UpdateAsync(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(user);
        var token = tokenGenerator.CreateAccessToken(
            user.Id,
            user.Email ?? string.Empty,
            roles,
            organization.Id,
            now,
            out var expires);

        return new WorkspaceSucceeded(new AuthResponse(
            token,
            new DateTimeOffset(expires, TimeSpan.Zero),
            "Bearer"));
    }

    public async Task<WorkspaceOutcome> JoinAsync(
        Guid userId,
        JoinWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new WorkspaceFailed(new Dictionary<string, string[]>
            {
                { "general", [ "User not found." ] }
            });
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(
            o => o.JoinCode == normalizedCode,
            cancellationToken);

        if (organization is null)
        {
            return new WorkspaceFailed(new Dictionary<string, string[]>
            {
                { "code", [ "Workspace not found for the provided join code." ] }
            });
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        user.OrganizationId = organization.Id;
        await userManager.UpdateAsync(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(user);
        var token = tokenGenerator.CreateAccessToken(
            user.Id,
            user.Email ?? string.Empty,
            roles,
            organization.Id,
            now,
            out var expires);

        return new WorkspaceSucceeded(new AuthResponse(
            token,
            new DateTimeOffset(expires, TimeSpan.Zero),
            "Bearer"));
    }

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
    {
        // Retry a few times; join codes are short.
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var code = GenerateJoinCode();
            var exists = await dbContext.Organizations.AnyAsync(
                o => o.JoinCode == code,
                cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        // Fallback (extremely unlikely) - still ensure it exists uniqueness by not checking.
        return GenerateJoinCode();
    }

    private static string GenerateJoinCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        var chars = new char[8];

        for (var i = 0; i < 8; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }
}

