using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Application.Auth;
using Asp.Versioning;

namespace TaskFlow.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Register a new user (assigned the User role). Sends a verification email; no JWT until verified.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterPendingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var outcome = await authService.RegisterAsync(request, cancellationToken);
        return outcome switch
        {
            RegisterPendingEmailVerification p => CreatedAtAction(
                nameof(Me),
                new { },
                new RegisterPendingResponse(p.Message)),
            RegisterFailed f => ValidationRegistrationErrors(f.Errors),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Confirm email using the token from the verification link.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var outcome = await authService.VerifyEmailAsync(request, cancellationToken);
        return outcome switch
        {
            VerifyEmailSucceeded s => Ok(s.Response),
            VerifyEmailFailed f => Problem(title: f.Title, detail: f.Detail, statusCode: f.StatusCode),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Resend the verification email (rate-limited; always returns 200).</summary>
    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ResendVerificationEmailAsync(request, cancellationToken);
        return Ok();
    }

    /// <summary>Request a password reset link (always returns 200 for enumeration safety).</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Complete password reset using the token from the email link.</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);

        var outcome = await authService.ResetPasswordAsync(request, cancellationToken);
        return outcome switch
        {
            ResetPasswordSucceeded s => Ok(new ResetPasswordResponse(s.Message)),
            ResetPasswordInvalidOrExpired => Problem(
                title: "Password reset failed",
                detail: "This reset link is invalid or has expired.",
                statusCode: StatusCodes.Status400BadRequest),
            ResetPasswordSameAsCurrent => SamePasswordValidationProblem(),
            ResetPasswordPasswordPolicyFailed f => PasswordPolicyValidationProblem(f.Errors),
            ResetPasswordServerError => Problem(
                title: "Password reset failed",
                detail: "Unable to complete password reset. Please try again or request a new reset link.",
                statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Exchange a valid refresh token for a new access token and refresh token (rotation).</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Refresh([FromBody] RefreshSessionRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);

        var outcome = await authService.RefreshSessionAsync(request, cancellationToken);
        return outcome switch
        {
            RefreshSessionSucceeded s => Ok(s.Response),
            RefreshSessionReuseDetected => Problem(
                title: "Unauthorized",
                detail: "Session invalidated due to suspicious activity. Please log in again.",
                statusCode: StatusCodes.Status401Unauthorized),
            RefreshSessionFailed f => Problem(title: f.Title, detail: f.Detail, statusCode: f.StatusCode),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Revoke the refresh token for this device/session.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await authService.LogoutAsync(userId.Value, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Revoke all refresh tokens for the current user.</summary>
    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await authService.LogoutAllAsync(userId.Value, cancellationToken);
        return NoContent();
    }

    /// <summary>List active refresh-token sessions. Optional body <c>refreshToken</c> marks the current device (POST avoids logging secrets on GET).</summary>
    [Authorize]
    [HttpPost("sessions/query")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSessionItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QuerySessions(
        [FromBody] GetSessionsRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var refresh = request?.RefreshToken;
        var sessions = await authService.GetSessionsAsync(userId.Value, refresh, cancellationToken);
        return Ok(sessions);
    }

    /// <summary>Revoke a specific session (refresh token row) by id.</summary>
    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var revoked = await authService.TryRevokeSessionAsync(userId.Value, sessionId, cancellationToken);
        return revoked ? NoContent() : NotFound();
    }

    /// <summary>Authenticate and receive a JWT bearer token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);

        var outcome = await authService.LoginAsync(request, cancellationToken);
        return outcome switch
        {
            LoginSucceeded s => Ok(s.Response),
            LoginEmailNotVerified => Problem(
                title: "Email not verified",
                detail: "Please check your inbox and verify your email.",
                statusCode: StatusCodes.Status403Forbidden),
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
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await authService.GetProfileAsync(userId.Value, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Change password. Revokes refresh tokens on other devices; this session stays signed in.</summary>
    [Authorize]
    [HttpPut("password")]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);

        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var outcome = await authService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        return outcome switch
        {
            ChangePasswordSucceeded s => Ok(new ChangePasswordResponse(s.Message)),
            ChangePasswordWrongCurrentPassword => Problem(
                title: "Invalid password",
                detail: "Current password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized),
            ChangePasswordNewSameAsCurrent => NewPasswordSameAsCurrentValidationProblem(),
            ChangePasswordInvalidRefresh => Problem(
                title: "Invalid refresh token",
                detail: "Provide a valid refresh token for this device.",
                statusCode: StatusCodes.Status400BadRequest),
            ChangePasswordPasswordPolicyFailed f => PasswordPolicyValidationProblem(f.Errors),
            ChangePasswordServerError => Problem(statusCode: StatusCodes.Status500InternalServerError),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>Update display name and/or user name (unique within your workspace).</summary>
    [Authorize]
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var outcome = await authService.UpdateProfileAsync(userId.Value, request, cancellationToken);
        return outcome switch
        {
            UpdateProfileSucceeded s => Ok(s.Response),
            UpdateProfileUserNameConflict => Problem(
                title: "Conflict",
                detail: "That username is already in use.",
                statusCode: StatusCodes.Status409Conflict),
            UpdateProfileServerError => Problem(statusCode: StatusCodes.Status500InternalServerError),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private Guid? TryGetUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdRaw, out var id) ? id : null;
    }

    private ActionResult PasswordPolicyValidationProblem(IReadOnlyDictionary<string, string[]> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var kvp in errors)
        {
            foreach (var message in kvp.Value)
            {
                modelState.AddModelError(kvp.Key, message);
            }
        }

        return ValidationProblem(modelState);
    }

    private ActionResult NewPasswordSameAsCurrentValidationProblem()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(
            nameof(ChangePasswordRequest.NewPassword),
            "New password must be different from your current password.");
        return ValidationProblem(modelState);
    }

    private ActionResult SamePasswordValidationProblem()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(
            nameof(ResetPasswordRequest.NewPassword),
            "Your new password must be different from your current password.");
        return ValidationProblem(modelState);
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
