using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Workspaces;

public sealed class WorkspaceService(
    UserManager<ApplicationUser> userManager,
    TaskFlowDbContext dbContext,
    IUserSessionIssuer sessionIssuer,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    INotificationService notificationService,
    IWebhookDispatcher webhookDispatcher) : IWorkspaceService
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
        user.WorkspaceRole = WorkspaceRole.Owner;
        user.WorkspaceJoinedAtUtc = now;
        await userManager.UpdateAsync(user);

        AuthResponse response;
        try
        {
            response = await sessionIssuer.IssueSessionAsync(user, GetConnectionInfo(), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new WorkspaceFailed(new Dictionary<string, string[]>
            {
                { "general", [ "Unable to issue a session for this user." ] }
            });
        }

        return new WorkspaceSucceeded(response);
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

        var joinedAt = timeProvider.GetUtcNow().UtcDateTime;
        user.OrganizationId = organization.Id;
        user.WorkspaceRole = WorkspaceRole.Member;
        user.WorkspaceJoinedAtUtc = joinedAt;
        await userManager.UpdateAsync(user);

        var admins = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.OrganizationId == organization.Id &&
                        (u.WorkspaceRole == WorkspaceRole.Owner || u.WorkspaceRole == WorkspaceRole.Admin))
            .ToListAsync(cancellationToken);

        var joinedName = user.DisplayName?.Trim() is { Length: > 0 } dn
            ? dn
            : user.UserName ?? user.Email ?? "A member";

        foreach (var admin in admins)
        {
            if (admin.Id == user.Id)
            {
                continue;
            }

            await notificationService.CreateAsync(
                admin.Id,
                "member.joined",
                "New member joined",
                $"{joinedName} joined your workspace",
                entityType: "Organization",
                entityId: organization.Id,
                ct: cancellationToken);
        }

        await webhookDispatcher.DispatchOrganizationEventAsync(
            organization.Id,
            WebhookEventTypes.MemberJoined,
            new
            {
                userId = user.Id,
                displayName = user.DisplayName,
                userName = user.UserName,
            },
            cancellationToken);

        AuthResponse response;
        try
        {
            response = await sessionIssuer.IssueSessionAsync(user, GetConnectionInfo(), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new WorkspaceFailed(new Dictionary<string, string[]>
            {
                { "general", [ "Unable to issue a session for this user." ] }
            });
        }

        return new WorkspaceSucceeded(response);
    }

    private SessionConnectionInfo? GetConnectionInfo()
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return null;
        }

        var ua = http.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(ua))
        {
            ua = null;
        }

        var ip = http.Connection.RemoteIpAddress?.ToString();
        return new SessionConnectionInfo(ua, ip);
    }

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var code = WorkspaceJoinCodes.Generate();
            var exists = await dbContext.Organizations.AnyAsync(
                o => o.JoinCode == code,
                cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        return WorkspaceJoinCodes.Generate();
    }
}
