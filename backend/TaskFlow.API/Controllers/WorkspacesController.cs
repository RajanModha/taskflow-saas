using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using TaskFlow.Domain.Entities;
using Asp.Versioning;

namespace TaskFlow.API.Controllers;

/// <summary>Manage workspace profile, members, and tags.</summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class WorkspacesController(
    IWorkspaceService workspaceService,
    IWorkspaceManagementService workspaceManagement,
    IWorkspaceTagService workspaceTagService,
    IWorkspaceWebhookService workspaceWebhookService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(MyWorkspaceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWorkspace(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await workspaceManagement.GetMyWorkspaceAsync(userId.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("tags")]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListTags(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var tags = await workspaceTagService.ListTagsAsync(userId.Value, cancellationToken);
        return tags is null ? NotFound() : Ok(tags);
    }

    [HttpPost("tags")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTag(
        [FromBody] CreateWorkspaceTagRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await workspaceTagService.CreateTagAsync(userId.Value, request, cancellationToken);
        return status == StatusCodes.Status201Created
            ? CreatedAtAction(nameof(ListTags), null, body)
            : StatusCode(status, body);
    }

    [HttpPut("tags/{tagId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTag(
        Guid tagId,
        [FromBody] UpdateWorkspaceTagRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await workspaceTagService.UpdateTagAsync(userId.Value, tagId, request, cancellationToken);
        return StatusCode(status, body);
    }

    [HttpDelete("tags/{tagId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTag(Guid tagId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var status = await workspaceTagService.DeleteTagAsync(userId.Value, tagId, cancellationToken);
        return status == StatusCodes.Status204NoContent ? NoContent() : NotFound();
    }

    [HttpGet("members")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(WorkspaceMembersPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        [FromQuery] string? role = null,
        CancellationToken cancellationToken = default)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        WorkspaceRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<WorkspaceRole>(role, ignoreCase: true, out var parsed))
            {
                return BadRequest(new { message = "Invalid role filter." });
            }

            roleFilter = parsed;
        }

        var result = await workspaceManagement.GetMembersPageAsync(
            userId.Value,
            page,
            pageSize,
            q,
            roleFilter,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("invite")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> InviteMember(
        [FromBody] InviteMemberRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await workspaceManagement.InviteMemberAsync(userId.Value, request, cancellationToken);
        return StatusCode(status, body);
    }

    [HttpPost("invite/resend")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> ResendInvite(
        [FromBody] ResendInviteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await workspaceManagement.ResendInviteAsync(userId.Value, request, cancellationToken);
        return StatusCode(status, body);
    }

    [HttpGet("invites")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceInviteRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInvites(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await workspaceManagement.ListInvitesAsync(userId.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("invites/{inviteId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CancelInvite(Guid inviteId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var status = await workspaceManagement.CancelInviteAsync(userId.Value, inviteId, cancellationToken);
        return status == StatusCodes.Status204NoContent ? NoContent() : NotFound();
    }

    [AllowAnonymous]
    [HttpPost("invites/accept")]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteRequest request,
        CancellationToken cancellationToken)
    {
        Guid? authId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            authId = TryGetUserId();
        }

        var (status, body) = await workspaceManagement.AcceptInviteAsync(request, authId, cancellationToken);
        return StatusCode(status, body);
    }

    [HttpPut("members/{memberId:guid}/role")]
    [Authorize(Policy = "OwnerPolicy")]
    public async Task<IActionResult> UpdateMemberRole(
        Guid memberId,
        [FromBody] UpdateMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, error) = await workspaceManagement.UpdateMemberRoleAsync(
            userId.Value,
            memberId,
            request,
            cancellationToken);

        return status switch
        {
            StatusCodes.Status200OK => Ok(),
            StatusCodes.Status400BadRequest => BadRequest(new { message = error }),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(new { message = error }),
            _ => Problem(statusCode: status),
        };
    }

    [HttpDelete("members/{memberId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> RemoveMember(Guid memberId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var status = await workspaceManagement.RemoveMemberAsync(userId.Value, memberId, cancellationToken);
        return status switch
        {
            StatusCodes.Status204NoContent => NoContent(),
            StatusCodes.Status400BadRequest => BadRequest(new { message = "Cannot remove the last owner from the workspace." }),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => Problem(statusCode: status),
        };
    }

    [HttpPost("invite-code/regenerate")]
    [Authorize(Policy = "OwnerPolicy")]
    [ProducesResponseType(typeof(RegenerateJoinCodeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegenerateJoinCode(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body, error) = await workspaceManagement.RegenerateJoinCodeAsync(userId.Value, cancellationToken);
        return status switch
        {
            StatusCodes.Status200OK => Ok(body),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(new { message = error }),
            _ => Problem(statusCode: status, detail: error),
        };
    }

    [HttpPut]
    [Authorize(Policy = "OwnerPolicy")]
    [ProducesResponseType(typeof(UpdateWorkspaceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateWorkspace(
        [FromBody] UpdateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body, error) = await workspaceManagement.UpdateWorkspaceNameAsync(
            userId.Value,
            request,
            cancellationToken);

        return status switch
        {
            StatusCodes.Status200OK => Ok(body),
            StatusCodes.Status400BadRequest => BadRequest(new { message = error }),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(new { message = error }),
            _ => Problem(statusCode: status, detail: error),
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var outcome = await workspaceService.CreateAsync(userId.Value, request, cancellationToken);

        return outcome switch
        {
            WorkspaceSucceeded s => Ok(s.Response),
            WorkspaceFailed f => ValidationWorkspaceErrors(f.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    [HttpGet("webhooks")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(IReadOnlyList<WebhookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListWebhooks(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await workspaceWebhookService.ListWebhooksAsync(userId.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("webhooks")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(WebhookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateWebhook(
        [FromBody] CreateWorkspaceWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await workspaceWebhookService.CreateWebhookAsync(userId.Value, request, cancellationToken);
        return status == StatusCodes.Status201Created
            ? CreatedAtAction(nameof(ListWebhooks), null, body)
            : StatusCode(status, body);
    }

    [HttpPut("webhooks/{webhookId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(WebhookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWebhook(
        Guid webhookId,
        [FromBody] UpdateWorkspaceWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await workspaceWebhookService.UpdateWebhookAsync(
            userId.Value,
            webhookId,
            request,
            cancellationToken);
        return StatusCode(status, body);
    }

    [HttpDelete("webhooks/{webhookId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWebhook(Guid webhookId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var status = await workspaceWebhookService.DeleteWebhookAsync(userId.Value, webhookId, cancellationToken);
        return status == StatusCodes.Status204NoContent ? NoContent() : NotFound();
    }

    [HttpGet("webhooks/{webhookId:guid}/deliveries")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(PagedResultDto<WebhookDeliveryLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhookDeliveries(
        Guid webhookId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await workspaceWebhookService.GetDeliveriesPageAsync(
            userId.Value,
            webhookId,
            page,
            pageSize,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("webhooks/{webhookId:guid}/test")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(WebhookTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestWebhook(Guid webhookId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await workspaceWebhookService.TestWebhookAsync(userId.Value, webhookId, cancellationToken);
        return StatusCode(status, body);
    }

    [HttpPost("join")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Join([FromBody] JoinWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var outcome = await workspaceService.JoinAsync(userId.Value, request, cancellationToken);

        return outcome switch
        {
            WorkspaceSucceeded s => Ok(s.Response),
            WorkspaceFailed f => ValidationWorkspaceErrors(f.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private Guid? TryGetUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (Guid.TryParse(userIdRaw, out var userId))
        {
            return userId;
        }

        return null;
    }

    private ActionResult ValidationWorkspaceErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var kvp in errors)
        {
            foreach (var message in kvp.Value)
            {
                ModelState.AddModelError(kvp.Key, message);
            }
        }

        return ValidationProblem(ModelState);
    }
}
