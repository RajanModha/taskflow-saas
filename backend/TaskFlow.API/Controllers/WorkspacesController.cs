using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Workspaces;
using Asp.Versioning;

namespace TaskFlow.API.Controllers;

/// <summary>Manage workspace profile, members, and tags.</summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class WorkspacesController(IMediator mediator) : ControllerBase
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

        var result = await mediator.Send(new GetMyWorkspaceQuery(userId.Value), cancellationToken);
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

        var tags = await mediator.Send(new ListWorkspaceTagsQuery(userId.Value), cancellationToken);
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

        var (status, body) = await mediator.Send(new CreateWorkspaceTagCommand(userId.Value, request), cancellationToken);
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

        var (status, body) = await mediator.Send(
            new UpdateWorkspaceTagCommand(userId.Value, tagId, request),
            cancellationToken);
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

        var status = await mediator.Send(new DeleteWorkspaceTagCommand(userId.Value, tagId), cancellationToken);
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

        var outcome = await mediator.Send(
            new GetWorkspaceMembersPageQuery(userId.Value, page, pageSize, q, role),
            cancellationToken);

        return outcome switch
        {
            WorkspaceMembersPageOk ok => Ok(ok.Value),
            WorkspaceMembersPageNotFoundOutcome => NotFound(),
            WorkspaceMembersPageBadRequestOutcome bad => BadRequest(new { message = bad.Message }),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
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

        var (status, body) = await mediator.Send(new InviteWorkspaceMemberCommand(userId.Value, request), cancellationToken);
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

        var (status, body) = await mediator.Send(new ResendWorkspaceInviteCommand(userId.Value, request), cancellationToken);
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

        var result = await mediator.Send(new ListWorkspaceInvitesQuery(userId.Value), cancellationToken);
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

        var status = await mediator.Send(new CancelWorkspaceInviteCommand(userId.Value, inviteId), cancellationToken);
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

        var (status, body) = await mediator.Send(new AcceptWorkspaceInviteCommand(request, authId), cancellationToken);
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

        var (status, error) = await mediator.Send(
            new UpdateWorkspaceMemberRoleCommand(userId.Value, memberId, request),
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

        var status = await mediator.Send(new RemoveWorkspaceMemberCommand(userId.Value, memberId), cancellationToken);
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

        var (status, body, error) = await mediator.Send(new RegenerateWorkspaceJoinCodeCommand(userId.Value), cancellationToken);
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

        var (status, body, error) = await mediator.Send(
            new UpdateWorkspaceProfileCommand(userId.Value, request),
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

        var outcome = await mediator.Send(new CreateWorkspaceCommand(userId.Value, request), cancellationToken);

        return outcome switch
        {
            WorkspaceSucceeded s => Ok(s.Response),
            WorkspaceFailed f => ValidationWorkspaceErrors(f.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    [HttpGet("task-templates")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListTaskTemplates(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ListWorkspaceTaskTemplatesQuery(userId.Value), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("task-templates/{templateId:guid}")]
    [ProducesResponseType(typeof(TaskTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskTemplate(Guid templateId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new GetWorkspaceTaskTemplateQuery(userId.Value, templateId),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("task-templates")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(TaskTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTaskTemplate(
        [FromBody] CreateTaskTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await mediator.Send(
            new CreateWorkspaceTaskTemplateCommand(userId.Value, request),
            cancellationToken);
        return status == StatusCodes.Status201Created
            ? CreatedAtAction(nameof(GetTaskTemplate), new { templateId = ((TaskTemplateDto)body!).Id }, body)
            : StatusCode(status, body);
    }

    [HttpPut("task-templates/{templateId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(TaskTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTaskTemplate(
        Guid templateId,
        [FromBody] UpdateTaskTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await mediator.Send(
            new UpdateWorkspaceTaskTemplateCommand(userId.Value, templateId, request),
            cancellationToken);
        return StatusCode(status, body);
    }

    [HttpDelete("task-templates/{templateId:guid}")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTaskTemplate(Guid templateId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var status = await mediator.Send(
            new DeleteWorkspaceTaskTemplateCommand(userId.Value, templateId),
            cancellationToken);
        return status == StatusCodes.Status204NoContent ? NoContent() : NotFound();
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

        var result = await mediator.Send(new ListWorkspaceWebhooksQuery(userId.Value), cancellationToken);
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

        var (status, body) = await mediator.Send(
            new CreateWorkspaceWebhookCommand(userId.Value, request),
            cancellationToken);
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

        var (status, body) = await mediator.Send(
            new UpdateWorkspaceWebhookCommand(userId.Value, webhookId, request),
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

        var status = await mediator.Send(new DeleteWorkspaceWebhookCommand(userId.Value, webhookId), cancellationToken);
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

        var result = await mediator.Send(
            new GetWorkspaceWebhookDeliveriesQuery(userId.Value, webhookId, page, pageSize),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("webhooks/{webhookId:guid}/test")]
    [Authorize(Policy = "AdminPolicy")]
    [ProducesResponseType(typeof(WebhookTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TestWebhook(Guid webhookId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var (status, body) = await mediator.Send(
            new TestWorkspaceWebhookCommand(userId.Value, webhookId),
            cancellationToken);
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

        var outcome = await mediator.Send(new JoinWorkspaceCommand(userId.Value, request), cancellationToken);

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
