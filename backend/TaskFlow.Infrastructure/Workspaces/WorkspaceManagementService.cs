using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Workspaces;

public sealed class WorkspaceManagementService(
    UserManager<ApplicationUser> userManager,
    TaskFlowDbContext dbContext,
    IEmailService emailService,
    IOptions<EmailSettings> emailSettings,
    TimeProvider timeProvider,
    IWebhookDispatcher webhookDispatcher) : IWorkspaceManagementService
{
    private readonly EmailSettings _email = emailSettings.Value;

    public async Task<MyWorkspaceResponse?> GetMyWorkspaceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.OrganizationId == Guid.Empty)
        {
            return null;
        }

        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == user.OrganizationId, cancellationToken);
        if (org is null)
        {
            return null;
        }

        var memberCount = await dbContext.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.OrganizationId == user.OrganizationId, cancellationToken);

        return new MyWorkspaceResponse(
            org.Id,
            org.Name,
            memberCount,
            org.JoinCode,
            new DateTimeOffset(org.CreatedAtUtc, TimeSpan.Zero),
            WorkspaceRoleStrings.ToApiString(user.WorkspaceRole));
    }

    public async Task<WorkspaceMembersPageResponse?> GetMembersPageAsync(
        Guid userId,
        int page,
        int pageSize,
        string? q,
        WorkspaceRole? roleFilter,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrganizationIdAsync(userId, cancellationToken);
        if (orgId is null)
        {
            return null;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.OrganizationId == orgId.Value);

        if (roleFilter is not null)
        {
            query = query.Where(u => u.WorkspaceRole == roleFilter);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var normalized = term.ToUpperInvariant();
            query = query.Where(u =>
                (u.NormalizedUserName != null && u.NormalizedUserName.Contains(normalized)) ||
                (u.NormalizedEmail != null && u.NormalizedEmail.Contains(normalized)) ||
                (u.DisplayName != null && EF.Functions.ILike(u.DisplayName, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new WorkspaceMemberRowDto(
                u.Id,
                u.UserName ?? string.Empty,
                u.DisplayName,
                u.Email ?? string.Empty,
                u.WorkspaceRole.ToString(),
                new DateTimeOffset(u.WorkspaceJoinedAtUtc, TimeSpan.Zero)))
            .ToListAsync(cancellationToken);

        return new WorkspaceMembersPageResponse(items, page, pageSize, total);
    }

    public async Task<(int StatusCode, object Body)> InviteMemberAsync(
        Guid actorUserId,
        InviteMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadActorInTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        if (!WorkspaceRoleStrings.TryParseInviteRole(request.Role, out var inviteRole))
        {
            return (StatusCodes.Status400BadRequest, new { message = "Role must be Admin or Member." });
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail.Length == 0)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Email is required." });
        }

        var alreadyMember = await dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(
                u => u.OrganizationId == actor.OrganizationId && u.NormalizedEmail == normalizedEmail,
                cancellationToken);
        if (alreadyMember)
        {
            return (StatusCodes.Status409Conflict, new { message = "This user is already a member of the workspace." });
        }

        var pendingDuplicate = await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .AnyAsync(
                i => i.OrganizationId == actor.OrganizationId &&
                     i.NormalizedEmail == normalizedEmail &&
                     i.AcceptedAtUtc == null &&
                     i.ExpiresAtUtc > timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);
        if (pendingDuplicate)
        {
            return (StatusCodes.Status409Conflict, new { message = "An active invite already exists for this email." });
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var (raw, hash) = RefreshTokenCrypto.GenerateToken();
        var org = await dbContext.Organizations.AsNoTracking()
            .FirstAsync(o => o.Id == actor.OrganizationId, cancellationToken);
        var inviterName = actor.DisplayName?.Trim() is { Length: > 0 } dn ? dn : actor.UserName ?? actor.Email ?? "Someone";

        var entity = new PendingInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = inviteRole,
            TokenHash = hash,
            ExpiresAtUtc = now.AddDays(7),
            SentAtUtc = now,
            ResendCount = 0,
            LastResentAtUtc = null,
            AcceptedAtUtc = null,
        };

        await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .Where(
                e => e.OrganizationId == actor.OrganizationId &&
                     e.NormalizedEmail == normalizedEmail &&
                     e.AcceptedAtUtc == null &&
                     e.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.PendingInvites.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var joinUrl = BuildJoinInviteUrl(raw);
        await emailService.SendEmailAsync(
            entity.Email,
            entity.Email,
            $"You're invited to {org.Name} on TaskFlow",
            EmailTemplates.WorkspaceInvite(inviterName, org.Name, joinUrl, WorkspaceRoleStrings.ToApiString(inviteRole)),
            "WorkspaceInvite",
            cancellationToken);

        return (StatusCodes.Status200OK, new InviteMemberResponse("Invite sent."));
    }

    public async Task<(int StatusCode, object Body)> ResendInviteAsync(
        Guid actorUserId,
        ResendInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadActorInTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "Workspace not found." });
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var invite = await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                i => i.OrganizationId == actor.OrganizationId &&
                     i.NormalizedEmail == normalizedEmail &&
                     i.AcceptedAtUtc == null,
                cancellationToken);

        if (invite is null)
        {
            return (StatusCodes.Status404NotFound, new { message = "No pending invite found for this email." });
        }

        if (invite.ExpiresAtUtc <= now)
        {
            return (StatusCodes.Status400BadRequest, new { message = "This invite has expired." });
        }

        if (invite.ResendCount >= 5)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Maximum resend attempts reached for this invite." });
        }

        var lastSend = invite.LastResentAtUtc ?? invite.SentAtUtc;
        if (lastSend > now.AddMinutes(-10))
        {
            return (StatusCodes.Status429TooManyRequests, new { message = "Please wait before resending this invite." });
        }

        var (raw, hash) = RefreshTokenCrypto.GenerateToken();
        invite.TokenHash = hash;
        invite.ResendCount++;
        invite.LastResentAtUtc = now;
        invite.ExpiresAtUtc = now.AddDays(7);

        var org = await dbContext.Organizations.AsNoTracking()
            .FirstAsync(o => o.Id == actor.OrganizationId, cancellationToken);
        var inviterName = actor.DisplayName?.Trim() is { Length: > 0 } dn ? dn : actor.UserName ?? actor.Email ?? "Someone";

        await dbContext.SaveChangesAsync(cancellationToken);

        var joinUrl = BuildJoinInviteUrl(raw);
        await emailService.SendEmailAsync(
            invite.Email,
            invite.Email,
            $"You're invited to {org.Name} on TaskFlow",
            EmailTemplates.WorkspaceInvite(
                inviterName,
                org.Name,
                joinUrl,
                WorkspaceRoleStrings.ToApiString(invite.Role)),
            "WorkspaceInviteResend",
            cancellationToken);

        return (StatusCodes.Status200OK, new ResendInviteResponse("Invite resent."));
    }

    public async Task<IReadOnlyList<WorkspaceInviteRowDto>?> ListInvitesAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrganizationIdAsync(actorUserId, cancellationToken);
        if (orgId is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var list = await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.OrganizationId == orgId)
            .OrderByDescending(i => i.SentAtUtc)
            .Select(i => new { i.Id, i.Email, i.Role, i.SentAtUtc, i.ExpiresAtUtc, i.ResendCount, i.AcceptedAtUtc })
            .ToListAsync(cancellationToken);

        return list
            .Select(i =>
            {
                string status;
                if (i.AcceptedAtUtc is not null)
                {
                    status = "Accepted";
                }
                else if (i.ExpiresAtUtc <= now)
                {
                    status = "Expired";
                }
                else
                {
                    status = "Pending";
                }

                return new WorkspaceInviteRowDto(
                    i.Id,
                    i.Email,
                    WorkspaceRoleStrings.ToApiString(i.Role),
                    new DateTimeOffset(i.SentAtUtc, TimeSpan.Zero),
                    new DateTimeOffset(i.ExpiresAtUtc, TimeSpan.Zero),
                    i.ResendCount,
                    status);
            })
            .ToList();
    }

    public async Task<int> CancelInviteAsync(Guid actorUserId, Guid inviteId, CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrganizationIdAsync(actorUserId, cancellationToken);
        if (orgId is null)
        {
            return StatusCodes.Status404NotFound;
        }

        var deleted = await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .Where(i => i.Id == inviteId && i.OrganizationId == orgId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0 ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
    }

    public async Task<(int StatusCode, object Body)> AcceptInviteAsync(
        AcceptInviteRequest request,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        var hash = RefreshTokenCrypto.HashRaw(request.Token.Trim());
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var invite = await dbContext.PendingInvites
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.TokenHash == hash, cancellationToken);

        if (invite is null)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Invalid or expired invite." });
        }

        if (invite.AcceptedAtUtc is not null)
        {
            return (StatusCodes.Status400BadRequest, new { message = "This invite has already been accepted." });
        }

        if (invite.ExpiresAtUtc <= now)
        {
            return (StatusCodes.Status400BadRequest, new { message = "This invite has expired." });
        }

        var org = await dbContext.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == invite.OrganizationId, cancellationToken);
        if (org is null)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Workspace no longer exists." });
        }

        ApplicationUser? targetUser = null;
        if (authenticatedUserId is { } authId)
        {
            targetUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == authId, cancellationToken);
            if (targetUser is null ||
                !string.Equals(targetUser.NormalizedEmail, invite.NormalizedEmail, StringComparison.Ordinal))
            {
                return (StatusCodes.Status403Forbidden, new { message = "Sign in as the invited email address to accept this invite." });
            }
        }
        else
        {
            targetUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == invite.NormalizedEmail, cancellationToken);
        }

        if (targetUser is null)
        {
            return (
                StatusCodes.Status200OK,
                new AcceptInviteRequiresRegistrationResponse(true, invite.Email, org.Name));
        }

        if (targetUser.OrganizationId == invite.OrganizationId)
        {
            return (StatusCodes.Status409Conflict, new { message = "You are already a member of this workspace." });
        }

        targetUser.OrganizationId = invite.OrganizationId;
        targetUser.WorkspaceRole = invite.Role;
        targetUser.WorkspaceJoinedAtUtc = now;
        var update = await userManager.UpdateAsync(targetUser);
        if (!update.Succeeded)
        {
            return (StatusCodes.Status400BadRequest, new { message = "Unable to accept invite for this account." });
        }

        invite.AcceptedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        await webhookDispatcher.DispatchOrganizationEventAsync(
            invite.OrganizationId,
            WebhookEventTypes.MemberJoined,
            new
            {
                userId = targetUser.Id,
                displayName = targetUser.DisplayName,
                userName = targetUser.UserName,
            },
            cancellationToken);

        return (StatusCodes.Status200OK, new AcceptInviteJoinedResponse("You have joined the workspace."));
    }

    public async Task<(int StatusCode, string? Error)> UpdateMemberRoleAsync(
        Guid actorUserId,
        Guid memberId,
        UpdateMemberRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadActorInTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, "Workspace not found.");
        }

        if (actor.WorkspaceRole != WorkspaceRole.Owner)
        {
            return (StatusCodes.Status403Forbidden, "Only the workspace owner can change member roles.");
        }

        if (memberId == actorUserId)
        {
            return (StatusCodes.Status400BadRequest, "You cannot change your own role.");
        }

        if (!WorkspaceRoleStrings.TryParseInviteRole(request.Role, out var newRole))
        {
            return (StatusCodes.Status400BadRequest, "Role must be Admin or Member.");
        }

        var member = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Id == memberId && u.OrganizationId == actor.OrganizationId,
                cancellationToken);
        if (member is null)
        {
            return (StatusCodes.Status404NotFound, "Member not found.");
        }

        if (member.WorkspaceRole == WorkspaceRole.Owner)
        {
            return (StatusCodes.Status400BadRequest, "The workspace owner role cannot be changed here.");
        }

        member.WorkspaceRole = newRole;
        await userManager.UpdateAsync(member);

        return (StatusCodes.Status200OK, null);
    }

    public async Task<int> RemoveMemberAsync(Guid actorUserId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var actor = await LoadActorInTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return StatusCodes.Status404NotFound;
        }

        var member = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Id == memberId && u.OrganizationId == actor.OrganizationId,
                cancellationToken);
        if (member is null)
        {
            return StatusCodes.Status404NotFound;
        }

        if (member.WorkspaceRole == WorkspaceRole.Owner && actor.WorkspaceRole != WorkspaceRole.Owner)
        {
            return StatusCodes.Status403Forbidden;
        }

        if (memberId == actorUserId &&
            member.WorkspaceRole == WorkspaceRole.Owner)
        {
            var ownerCount = await dbContext.Users
                .IgnoreQueryFilters()
                .CountAsync(
                    u => u.OrganizationId == actor.OrganizationId && u.WorkspaceRole == WorkspaceRole.Owner,
                    cancellationToken);
            if (ownerCount <= 1)
            {
                return StatusCodes.Status400BadRequest;
            }
        }

        var orgId = member.OrganizationId;
        await dbContext.Tasks
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == orgId && t.AssigneeId == memberId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.AssigneeId, (Guid?)null), cancellationToken);

        member.OrganizationId = Guid.Empty;
        member.WorkspaceRole = WorkspaceRole.Member;
        member.WorkspaceJoinedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await userManager.UpdateAsync(member);

        return StatusCodes.Status204NoContent;
    }

    public async Task<(int StatusCode, object? Body, string? Error)> RegenerateJoinCodeAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadActorInTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, null, "Workspace not found.");
        }

        if (actor.WorkspaceRole != WorkspaceRole.Owner)
        {
            return (StatusCodes.Status403Forbidden, null, "Only the workspace owner can regenerate the join code.");
        }

        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == actor.OrganizationId, cancellationToken);
        if (org is null)
        {
            return (StatusCodes.Status404NotFound, null, "Workspace not found.");
        }

        org.JoinCode = await GenerateUniqueJoinCodeAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (StatusCodes.Status200OK, new RegenerateJoinCodeResponse(org.JoinCode), null);
    }

    public async Task<(int StatusCode, object? Body, string? Error)> UpdateWorkspaceNameAsync(
        Guid actorUserId,
        UpdateWorkspaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await LoadActorInTenantAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            return (StatusCodes.Status404NotFound, null, "Workspace not found.");
        }

        if (actor.WorkspaceRole != WorkspaceRole.Owner)
        {
            return (StatusCodes.Status403Forbidden, null, "Only the workspace owner can rename the workspace.");
        }

        var name = request.Name.Trim();
        if (name.Length == 0 || name.Length > 128)
        {
            return (StatusCodes.Status400BadRequest, null, "Name must be between 1 and 128 characters.");
        }

        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == actor.OrganizationId, cancellationToken);
        if (org is null)
        {
            return (StatusCodes.Status404NotFound, null, "Workspace not found.");
        }

        org.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (StatusCodes.Status200OK, new UpdateWorkspaceResponse("Workspace updated."), null);
    }

    private async Task<ApplicationUser?> LoadActorInTenantAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.OrganizationId == Guid.Empty)
        {
            return null;
        }

        if (user.WorkspaceRole != WorkspaceRole.Owner && user.WorkspaceRole != WorkspaceRole.Admin)
        {
            return null;
        }

        return user;
    }

    private async Task<Guid?> GetUserOrganizationIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await LoadActorInTenantAsync(userId, cancellationToken);
        return user?.OrganizationId;
    }

    private string BuildJoinInviteUrl(string rawToken)
    {
        var baseUrl = _email.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/join?token={Uri.EscapeDataString(rawToken)}";
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var code = WorkspaceJoinCodes.Generate();
            var exists = await dbContext.Organizations.AnyAsync(o => o.JoinCode == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        return WorkspaceJoinCodes.Generate();
    }
}
