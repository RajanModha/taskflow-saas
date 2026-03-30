using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Workspaces;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class WorkspacesController(IWorkspaceService workspaceService) : ControllerBase
{
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

