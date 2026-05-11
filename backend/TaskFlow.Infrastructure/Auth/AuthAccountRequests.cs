using System.Data;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Auth;

public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    IEmailService emailService,
    IOptions<EmailSettings> emailSettings)
    : IRequestHandler<RegisterCommand, RegisterOutcome>
{
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task<RegisterOutcome> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();

        var emailExists = await authRepository.EmailExistsAsync(normalizedEmail, cancellationToken);
        if (emailExists)
        {
            return AuthRequestCommon.ToRegisterFailure(
                IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "Email is already taken.",
                }));
        }

        var userNameExists = await authRepository.UserNameExistsAsync(normalizedUserName, cancellationToken);
        if (userNameExists)
        {
            return AuthRequestCommon.ToRegisterFailure(
                IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateUserName",
                    Description = "User name is already taken.",
                }));
        }

        var orgId = Guid.NewGuid();
        var organization = new TaskFlow.Domain.Entities.Organization
        {
            Id = orgId,
            Name = request.OrganizationName,
            JoinCode = AuthRequestCommon.GenerateJoinCode(),
            CreatedAtUtc = now,
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = false,
            EmailVerified = false,
            CreatedAtUtc = now,
            OrganizationId = orgId,
            WorkspaceRole = TaskFlow.Domain.Entities.WorkspaceRole.Owner,
            WorkspaceJoinedAtUtc = now,
        };

        var rawVerification = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        return await authRepository.WithTransactionAsync(async ct =>
        {
            await authRepository.AddOrganizationAsync(organization, ct);
            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return (false, (RegisterOutcome)AuthRequestCommon.ToRegisterFailure(createResult));
            }

            var roleResult = await userManager.AddToRoleAsync(user, DomainRoles.User);
            if (!roleResult.Succeeded)
            {
                return (false, (RegisterOutcome)AuthRequestCommon.ToRegisterFailure(roleResult));
            }

            var verificationHashHex = AuthRequestCommon.HashVerificationToken(rawVerification);
            user.EmailVerificationToken = verificationHashHex;
            user.EmailVerificationTokenExpiry = now.AddHours(24);
            user.VerificationResendCount = 0;

            var updateVerification = await userManager.UpdateAsync(user);
            if (!updateVerification.Succeeded)
            {
                return (false, (RegisterOutcome)AuthRequestCommon.ToRegisterFailure(updateVerification));
            }
            var baseUrl = _emailSettings.FrontendBaseUrl.TrimEnd('/');
            var verifyUrl = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(rawVerification)}";
            await emailService.SendEmailAsync(
                user.Email!,
                user.UserName ?? user.Email!,
                "Verify your TaskFlow email",
                EmailTemplates.VerifyEmail(user.UserName ?? user.Email!, verifyUrl),
                "RegisterEmailVerification",
                ct);

            return (true, (RegisterOutcome)new RegisterPendingEmailVerification("Check your email to verify your account."));
        }, cancellationToken);
    }
}

public sealed class VerifyEmailCommandHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    IUserSessionIssuer sessionIssuer,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<VerifyEmailCommand, VerifyEmailOutcome>
{
    public async Task<VerifyEmailOutcome> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hashHex = AuthRequestCommon.HashVerificationToken(request.Token.Trim());

        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.EmailVerificationToken != null && u.EmailVerificationToken == hashHex,
                cancellationToken);

        if (user is null || !AuthRequestCommon.TokenHashesEqual(user.EmailVerificationToken, hashHex))
        {
            return new VerifyEmailFailed(
                "Email verification failed",
                "This verification link is invalid or has expired.",
                StatusCodes.Status400BadRequest);
        }

        if (user.EmailVerificationTokenExpiry is null || user.EmailVerificationTokenExpiry <= now)
        {
            return new VerifyEmailFailed(
                "Email verification failed",
                "This verification link is invalid or has expired.",
                StatusCodes.Status400BadRequest);
        }

        var organization = await authRepository.GetOrganizationByIdAsync(user.OrganizationId, cancellationToken);

        user.EmailVerified = true;
        user.EmailConfirmed = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.LastVerificationResendAt = null;
        user.VerificationResendCount = 0;

        AuthResponse response;
        try
        {
            response = await sessionIssuer.IssueSessionAsync(user, AuthRequestCommon.GetSessionConnectionInfo(httpContextAccessor), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new VerifyEmailFailed(
                "Email verification failed",
                "This verification link is invalid or has expired.",
                StatusCodes.Status400BadRequest);
        }

        await emailService.SendEmailAsync(
            user.Email!,
            user.UserName ?? user.Email!,
            "Welcome to TaskFlow!",
            EmailTemplates.WelcomeEmail(user.UserName ?? user.Email!, organization?.Name ?? "your workspace"),
            "EmailWelcome",
            cancellationToken);

        return new VerifyEmailSucceeded(response);
    }
}

public sealed class ResendVerificationEmailCommandHandler(
    TimeProvider timeProvider,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IOptions<EmailSettings> emailSettings) : IRequestHandler<ResendVerificationEmailCommand>
{
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task<Unit> Handle(ResendVerificationEmailCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || user.EmailVerified)
        {
            return Unit.Value;
        }

        if (user.LastVerificationResendAt is not null &&
            now < user.LastVerificationResendAt.Value.AddMinutes(20))
        {
            return Unit.Value;
        }

        if (user.VerificationResendCount >= 3)
        {
            return Unit.Value;
        }

        var rawVerification = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.EmailVerificationToken = AuthRequestCommon.HashVerificationToken(rawVerification);
        user.EmailVerificationTokenExpiry = now.AddHours(24);
        user.LastVerificationResendAt = now;
        user.VerificationResendCount++;

        await userManager.UpdateAsync(user);

        var baseUrl = _emailSettings.FrontendBaseUrl.TrimEnd('/');
        var verifyUrl = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(rawVerification)}";
        await emailService.SendEmailAsync(
            user.Email!,
            user.UserName ?? user.Email!,
            "Verify your TaskFlow email",
            EmailTemplates.VerifyEmail(user.UserName ?? user.Email!, verifyUrl),
            "EmailVerificationResend",
            cancellationToken);

        return Unit.Value;
    }
}

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IUserSessionIssuer sessionIssuer,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<LoginCommand, LoginOutcome>
{
    public async Task<LoginOutcome> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        static Task DelayOnFailureAsync(CancellationToken ct) => Task.Delay(Random.Shared.Next(50, 200), ct);

        var request = command.Request;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("Invalid email or password.");
        }

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("Invalid email or password.");
        }

        if (!user.EmailVerified)
        {
            return new LoginEmailNotVerified();
        }

        if (user.OrganizationId == Guid.Empty)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("User has not been assigned to a workspace.");
        }

        AuthResponse response;
        try
        {
            response = await sessionIssuer.IssueSessionAsync(user, AuthRequestCommon.GetSessionConnectionInfo(httpContextAccessor), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("User has not been assigned to a workspace.");
        }

        return new LoginSucceeded(response);
    }
}

public sealed class RefreshSessionCommandHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    IOptions<JwtSettings> jwtSettings,
    UserManager<ApplicationUser> userManager,
    IUserSessionIssuer sessionIssuer,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RefreshSessionCommandHandler> logger) : IRequestHandler<RefreshSessionCommand, RefreshSessionOutcome>
{
    private readonly JwtSettings _jwt = jwtSettings.Value;

    public async Task<RefreshSessionOutcome> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await RefreshSessionOnceAsync(command.Request, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts - 1 && IsPostgresTransientConcurrency(ex))
            {
                authRepository.ClearChangeTracker();
                logger.LogWarning(
                    ex,
                    "Refresh token rotation hit retriable DB concurrency (attempt {Attempt} of {Max}).",
                    attempt + 1,
                    maxAttempts);
            }
        }

        return new RefreshSessionFailed(
            "Service temporarily unavailable",
            "Could not refresh the session. Please try again.",
            StatusCodes.Status503ServiceUnavailable);
    }

    private async Task<RefreshSessionOutcome> RefreshSessionOnceAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var raw = request.RefreshToken.Trim();
        var hashHex = RefreshTokenCrypto.HashRaw(raw);

        return await authRepository.WithTransactionAsync(async ct =>
        {
            var stored = await authRepository.GetRefreshTokenByHashAsync(hashHex, ct);

            if (stored is null || !AuthRequestCommon.TokenHashesEqual(stored.TokenHash, hashHex))
            {
                return (false, (RefreshSessionOutcome)new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized));
            }

            var user = await userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);
            if (user is null || !user.EmailVerified || user.OrganizationId == Guid.Empty)
            {
                return (false, (RefreshSessionOutcome)new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized));
            }

            if (stored.RevokedAtUtc.HasValue)
            {
                await authRepository.RevokeAllActiveRefreshTokensForUserAsync(user.Id, now, ct);
                return (true, (RefreshSessionOutcome)new RefreshSessionReuseDetected());
            }

            if (stored.ExpiresAtUtc <= now)
            {
                return (false, (RefreshSessionOutcome)new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized));
            }

            var (rawNew, hashNew) = RefreshTokenCrypto.GenerateToken();
            var refreshDays = _jwt.RefreshTokenDays <= 0 ? 30 : _jwt.RefreshTokenDays;
            var newExpiryUtc = now.AddDays(refreshDays);

            await authRepository.MarkRefreshTokenRotatedAsync(stored.Id, now, hashNew, ct);

            AuthResponse response;
            try
            {
                response = await sessionIssuer.AttachRefreshSessionAsync(
                    user,
                    rawNew,
                    hashNew,
                    newExpiryUtc,
                    AuthRequestCommon.GetSessionConnectionInfo(httpContextAccessor),
                    ct);
            }
            catch (InvalidOperationException)
            {
                return (false, (RefreshSessionOutcome)new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized));
            }
            return (true, (RefreshSessionOutcome)new RefreshSessionSucceeded(response));
        }, cancellationToken, IsolationLevel.Serializable);
    }

    private static bool IsPostgresTransientConcurrency(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is PostgresException pe && (pe.SqlState == "40001" || pe.SqlState == "40P01"))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class ForgotPasswordCommandHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IOptions<EmailSettings> emailSettings) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private const string ForgotPasswordResponseMessage =
        "If that email is registered you'll receive a link shortly.";
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existing = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existing is null)
        {
            return new ForgotPasswordResponse(ForgotPasswordResponseMessage);
        }

        var userId = existing.Id;
        string? rawToken = null;
        string? sendEmail = null;
        string? sendName = null;

        return await authRepository.WithTransactionAsync(async ct =>
        {
            var user = await userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return (true, new ForgotPasswordResponse(ForgotPasswordResponseMessage));
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;

            if (user.PasswordResetHourStartedUtc is null ||
                now >= user.PasswordResetHourStartedUtc.Value.AddHours(1))
            {
                user.PasswordResetHourStartedUtc = now;
                user.PasswordResetRequestsThisHour = 0;
            }

            user.LastResetRequestAt = now;

            if (user.PasswordResetRequestsThisHour >= 3)
            {
                await userManager.UpdateAsync(user);
                return (true, new ForgotPasswordResponse(ForgotPasswordResponseMessage));
            }

            user.PasswordResetRequestsThisHour++;

            rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            user.PasswordResetToken = AuthRequestCommon.HashVerificationToken(rawToken);
            user.PasswordResetTokenExpiry = now.AddHours(1);
            user.PasswordResetUsed = false;

            await userManager.UpdateAsync(user);
            sendEmail = user.Email;
            sendName = user.UserName;
            if (rawToken is null || string.IsNullOrEmpty(sendEmail))
            {
                return (true, new ForgotPasswordResponse(ForgotPasswordResponseMessage));
            }

            var baseUrl = _emailSettings.FrontendBaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
            await emailService.SendEmailAsync(
                sendEmail,
                sendName ?? sendEmail,
                "Reset your TaskFlow password",
                EmailTemplates.ResetPassword(sendName ?? sendEmail, url),
                "PasswordReset",
                ct);

            return (true, new ForgotPasswordResponse(ForgotPasswordResponseMessage));
        }, cancellationToken, IsolationLevel.Serializable);
    }
}

public sealed class ResetPasswordCommandHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    UserManager<ApplicationUser> userManager,
    IPasswordHasher<ApplicationUser> passwordHasher,
    ILogger<ResetPasswordCommandHandler> logger) : IRequestHandler<ResetPasswordCommand, ResetPasswordOutcome>
{
    public async Task<ResetPasswordOutcome> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hashHex = AuthRequestCommon.HashVerificationToken(request.Token.Trim());

        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.PasswordResetToken != null && u.PasswordResetToken == hashHex,
                cancellationToken);

        if (user is null || !AuthRequestCommon.TokenHashesEqual(user.PasswordResetToken, hashHex))
        {
            return new ResetPasswordInvalidOrExpired();
        }

        if (user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry <= now)
        {
            return new ResetPasswordInvalidOrExpired();
        }

        if (user.PasswordResetUsed)
        {
            return new ResetPasswordInvalidOrExpired();
        }

        if (await userManager.CheckPasswordAsync(user, request.NewPassword))
        {
            return new ResetPasswordSameAsCurrent();
        }

        var passwordPolicy = await AuthRequestCommon.ValidatePasswordAgainstIdentityAsync(userManager, user, request.NewPassword, cancellationToken);
        if (!passwordPolicy.Succeeded)
        {
            return new ResetPasswordPasswordPolicyFailed(AuthRequestCommon.MapIdentityPasswordErrors(passwordPolicy));
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.PasswordResetUsed = true;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.SecurityStamp = Guid.NewGuid().ToString();

        await authRepository.RevokeAllActiveRefreshTokensForUserAsync(user.Id, now, cancellationToken);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            logger.LogWarning(
                "Password reset UpdateAsync failed for user {UserId}: {Errors}",
                user.Id,
                string.Join("; ", updateResult.Errors.Select(e => $"{e.Code}: {e.Description}")));
            return new ResetPasswordServerError();
        }

        return new ResetPasswordSucceeded("Password reset successfully. Please log in.");
    }
}

public sealed class ChangePasswordCommandHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    UserManager<ApplicationUser> userManager,
    IPasswordHasher<ApplicationUser> passwordHasher) : IRequestHandler<ChangePasswordCommand, ChangePasswordOutcome>
{
    public async Task<ChangePasswordOutcome> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;
        var request = command.Request;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return new ChangePasswordServerError();
        }

        var refreshHash = RefreshTokenCrypto.HashRaw(request.RefreshToken.Trim());
        var sessionValid = await authRepository.HasValidRefreshTokenAsync(userId, refreshHash, now, cancellationToken);
        if (!sessionValid)
        {
            return new ChangePasswordInvalidRefresh();
        }

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) ==
            PasswordVerificationResult.Failed)
        {
            return new ChangePasswordWrongCurrentPassword();
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.NewPassword) !=
            PasswordVerificationResult.Failed)
        {
            return new ChangePasswordNewSameAsCurrent();
        }

        return await authRepository.WithTransactionAsync(async ct =>
        {
            var changeResult = await userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);
            if (!changeResult.Succeeded)
            {
                return (false, (ChangePasswordOutcome)new ChangePasswordPasswordPolicyFailed(AuthRequestCommon.MapChangePasswordIdentityErrors(changeResult)));
            }

            await authRepository.RevokeOtherActiveRefreshTokensAsync(userId, refreshHash, now, ct);
            return (true, (ChangePasswordOutcome)new ChangePasswordSucceeded("Password updated. Other devices have been logged out."));
        }, cancellationToken);
    }
}

public sealed class UpdateProfileCommandHandler(
    IAuthRepository authRepository,
    UserManager<ApplicationUser> userManager,
    ILogger<UpdateProfileCommandHandler> logger) : IRequestHandler<UpdateProfileCommand, UpdateProfileOutcome>
{
    public async Task<UpdateProfileOutcome> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;
        var request = command.Request;
        var user = await userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return new UpdateProfileServerError();
        }

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var trimmedName = request.UserName.Trim();
            var normalized = trimmedName.ToUpperInvariant();
            if (normalized != user.NormalizedUserName)
            {
                var takenInOrg = await authRepository.UserNameTakenInOrganizationAsync(
                    user.OrganizationId,
                    userId,
                    normalized,
                    cancellationToken);
                if (takenInOrg)
                {
                    return new UpdateProfileUserNameConflict();
                }

                var setName = await userManager.SetUserNameAsync(user, trimmedName);
                if (!setName.Succeeded)
                {
                    if (setName.Errors.Any(e => e.Code == "DuplicateUserName"))
                    {
                        return new UpdateProfileUserNameConflict();
                    }

                    logger.LogWarning(
                        "SetUserNameAsync failed for user {UserId}: {Errors}",
                        user.Id,
                        string.Join("; ", setName.Errors.Select(e => $"{e.Code}: {e.Description}")));
                    return new UpdateProfileServerError();
                }
            }
        }

        if (request.DisplayName is not null)
        {
            var trimmedDisplay = request.DisplayName.Trim();
            user.DisplayName = trimmedDisplay.Length == 0 ? null : trimmedDisplay;
            var updateDisplay = await userManager.UpdateAsync(user);
            if (!updateDisplay.Succeeded)
            {
                logger.LogWarning(
                    "UpdateAsync failed after display name change for user {UserId}: {Errors}",
                    user.Id,
                    string.Join("; ", updateDisplay.Errors.Select(e => $"{e.Code}: {e.Description}")));
                return new UpdateProfileServerError();
            }
        }

        var refreshed = await userManager.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (refreshed is null)
        {
            return new UpdateProfileServerError();
        }
        var profile = await AuthRequestCommon.MapToProfileResponseAsync(authRepository, userManager, refreshed, cancellationToken);
        return new UpdateProfileSucceeded(profile);
    }
}

internal static class AuthRequestCommon
{
    public static async Task<UserProfileResponse> MapToProfileResponseAsync(
        IAuthRepository authRepository,
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (user.OrganizationId == Guid.Empty)
        {
            var emptyRoles = Array.Empty<string>();
            return new UserProfileResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty,
                emptyRoles,
                PickPrimaryRole(emptyRoles),
                Guid.Empty,
                string.Empty,
                string.Empty,
                user.DisplayName,
                user.AvatarUrl,
                new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero));
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var organization = await authRepository.GetOrganizationByIdAsync(user.OrganizationId, cancellationToken);

        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles,
            PickPrimaryRole(roles),
            user.OrganizationId,
            organization?.Name ?? string.Empty,
            organization?.JoinCode ?? string.Empty,
            user.DisplayName,
            user.AvatarUrl,
            new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero));
    }

    public static string PickPrimaryRole(IReadOnlyList<string> roles)
    {
        foreach (var role in roles)
        {
            if (string.Equals(role, DomainRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return DomainRoles.Admin;
            }
        }

        return roles.Count > 0 ? roles[0] : DomainRoles.User;
    }

    public static async Task<IdentityResult> ValidatePasswordAgainstIdentityAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            if (!result.Succeeded)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errors.ToArray());
    }

    public static IReadOnlyDictionary<string, string[]> MapIdentityPasswordErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(e => e.Description).ToArray();
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ResetPasswordRequest.NewPassword)] = messages,
        };
    }

    public static IReadOnlyDictionary<string, string[]> MapChangePasswordIdentityErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(e => e.Description).ToArray();
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ChangePasswordRequest.NewPassword)] = messages,
        };
    }

    public static string HashVerificationToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public static bool TokenHashesEqual(string? storedHex, string computedHex)
    {
        if (storedHex is null || storedHex.Length != computedHex.Length)
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(storedHex),
                Convert.FromHexString(computedHex));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string GenerateJoinCode()
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

    public static RegisterFailed ToRegisterFailure(IdentityResult result)
    {
        var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var error in result.Errors)
        {
            var key = error.Code switch
            {
                "DuplicateUserName" => nameof(RegisterRequest.UserName),
                "DuplicateEmail" => nameof(RegisterRequest.Email),
                "InvalidEmail" => nameof(RegisterRequest.Email),
                "PasswordTooShort" => nameof(RegisterRequest.Password),
                "PasswordRequiresDigit" => nameof(RegisterRequest.Password),
                "PasswordRequiresLower" => nameof(RegisterRequest.Password),
                "PasswordRequiresUpper" => nameof(RegisterRequest.Password),
                "PasswordRequiresNonAlphanumeric" => nameof(RegisterRequest.Password),
                _ => string.Empty,
            };

            if (string.IsNullOrEmpty(key))
            {
                key = "general";
            }

            if (!dict.TryGetValue(key, out var list))
            {
                list = [];
                dict[key] = list;
            }

            list.Add(error.Description);
        }

        return new RegisterFailed(dict.ToDictionary(k => k.Key, v => v.Value.ToArray()));
    }

    public static SessionConnectionInfo? GetSessionConnectionInfo(IHttpContextAccessor httpContextAccessor)
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
}
