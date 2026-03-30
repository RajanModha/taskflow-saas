using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth;
using Asp.Versioning;

namespace TaskFlow.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Register a new user (assigned the User role).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var outcome = await authService.RegisterAsync(request, cancellationToken);
        return outcome switch
        {
            RegisterSucceeded s => CreatedAtAction(nameof(Me), new { }, s.Response),
            RegisterFailed f => ValidationRegistrationErrors(f.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Authenticate and receive a JWT bearer token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var outcome = await authService.LoginAsync(request, cancellationToken);
        return outcome switch
        {
            LoginSucceeded s => Ok(s.Response),
            LoginFailed f => Problem(
                title: "Login failed",
                detail: f.Error,
                statusCode: StatusCodes.Status401Unauthorized),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Current user profile (requires Authorization: Bearer).</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var profile = await authService.GetProfileAsync(id, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    private ActionResult ValidationRegistrationErrors(IReadOnlyDictionary<string, string[]> errors)
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
