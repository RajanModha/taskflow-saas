using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Common;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    TaskFlowDbContext dbContext,
    TimeProvider timeProvider,
    IEmailService emailService,
    IOptions<EmailSettings> emailSettings,
    IUserSessionIssuer sessionIssuer,
    IPasswordHasher<ApplicationUser> passwordHasher,
    ILogger<AuthService> logger) : IAuthService
{
    private const string ForgotPasswordResponseMessage =
        "If that email is registered you'll receive a link shortly.";

    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task<RegisterOutcome> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();

        var emailExists = await dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            return ToRegisterFailure(
                IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "Email is already taken.",
                }));
        }

        var userNameExists = await dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
        if (userNameExists)
        {
            return ToRegisterFailure(
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
            JoinCode = GenerateJoinCode(),
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
        };

        var rawVerification = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.Organizations.AddAsync(organization, cancellationToken);

            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ToRegisterFailure(createResult);
            }

            var roleResult = await userManager.AddToRoleAsync(user, DomainRoles.User);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ToRegisterFailure(roleResult);
            }

            var verificationHashHex = HashVerificationToken(rawVerification);
            user.EmailVerificationToken = verificationHashHex;
            user.EmailVerificationTokenExpiry = now.AddHours(24);
            user.VerificationResendCount = 0;

            var updateVerification = await userManager.UpdateAsync(user);
            if (!updateVerification.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ToRegisterFailure(updateVerification);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var baseUrl = _emailSettings.FrontendBaseUrl.TrimEnd('/');
        var verifyUrl = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(rawVerification)}";
        await emailService.SendEmailAsync(
            user.Email!,
            user.UserName ?? user.Email!,
            "Verify your TaskFlow email",
            EmailTemplates.VerifyEmail(user.UserName ?? user.Email!, verifyUrl),
            "RegisterEmailVerification",
            cancellationToken);

        return new RegisterPendingEmailVerification("Check your email to verify your account.");
    }

    public async Task<VerifyEmailOutcome> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hashHex = HashVerificationToken(request.Token.Trim());

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.EmailVerificationToken != null && u.EmailVerificationToken == hashHex,
                cancellationToken);

        if (user is null || !TokenHashesEqual(user.EmailVerificationToken, hashHex))
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

        var organization = await dbContext.Organizations.FirstOrDefaultAsync(
            o => o.Id == user.OrganizationId,
            cancellationToken);

        user.EmailVerified = true;
        user.EmailConfirmed = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.LastVerificationResendAt = null;
        user.VerificationResendCount = 0;

        AuthResponse response;
        try
        {
            response = await sessionIssuer.IssueSessionAsync(user, cancellationToken);
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

    public async Task ResendVerificationEmailAsync(ResendVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || user.EmailVerified)
        {
            return;
        }

        if (user.LastVerificationResendAt is not null &&
            now < user.LastVerificationResendAt.Value.AddMinutes(20))
        {
            return;
        }

        if (user.VerificationResendCount >= 3)
        {
            return;
        }

        var rawVerification = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.EmailVerificationToken = HashVerificationToken(rawVerification);
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
    }

    public async Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return new LoginFailed("Invalid email or password.");
        }

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            return new LoginFailed("Invalid email or password.");
        }

        if (!user.EmailVerified)
        {
            return new LoginEmailNotVerified();
        }

        if (user.OrganizationId == Guid.Empty)
        {
            return new LoginFailed("User has not been assigned to a workspace.");
        }

        AuthResponse response;
        try
        {
            response = await sessionIssuer.IssueSessionAsync(user, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new LoginFailed("User has not been assigned to a workspace.");
        }

        return new LoginSucceeded(response);
    }

    public async Task<RefreshSessionOutcome> RefreshSessionAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var raw = request.RefreshToken.Trim();
        var hashHex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.RefreshTokenHash != null && u.RefreshTokenHash == hashHex,
                cancellationToken);

        if (user is null || !TokenHashesEqual(user.RefreshTokenHash, hashHex))
        {
            return new RefreshSessionFailed(
                "Invalid refresh token",
                "The refresh token is invalid or has been revoked.",
                StatusCodes.Status401Unauthorized);
        }

        if (user.RefreshTokenExpiryUtc is null || user.RefreshTokenExpiryUtc <= now)
        {
            return new RefreshSessionFailed(
                "Invalid refresh token",
                "The refresh token is invalid or has been revoked.",
                StatusCodes.Status401Unauthorized);
        }

        if (!user.EmailVerified || user.OrganizationId == Guid.Empty)
        {
            return new RefreshSessionFailed(
                "Invalid refresh token",
                "The refresh token is invalid or has been revoked.",
                StatusCodes.Status401Unauthorized);
        }

        AuthResponse response;
        try
        {
            response = await sessionIssuer.IssueSessionAsync(user, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new RefreshSessionFailed(
                "Invalid refresh token",
                "The refresh token is invalid or has been revoked.",
                StatusCodes.Status401Unauthorized);
        }

        return new RefreshSessionSucceeded(response);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existing = await dbContext.Users
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var user = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return new ForgotPasswordResponse(ForgotPasswordResponseMessage);
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
                await transaction.CommitAsync(cancellationToken);
                return new ForgotPasswordResponse(ForgotPasswordResponseMessage);
            }

            user.PasswordResetRequestsThisHour++;

            rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            user.PasswordResetToken = HashVerificationToken(rawToken);
            user.PasswordResetTokenExpiry = now.AddHours(1);
            user.PasswordResetUsed = false;

            await userManager.UpdateAsync(user);
            sendEmail = user.Email;
            sendName = user.UserName;
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (rawToken is null || string.IsNullOrEmpty(sendEmail))
        {
            return new ForgotPasswordResponse(ForgotPasswordResponseMessage);
        }

        var baseUrl = _emailSettings.FrontendBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        await emailService.SendEmailAsync(
            sendEmail,
            sendName ?? sendEmail,
            "Reset your TaskFlow password",
            EmailTemplates.ResetPassword(sendName ?? sendEmail, url),
            "PasswordReset",
            cancellationToken);

        return new ForgotPasswordResponse(ForgotPasswordResponseMessage);
    }

    public async Task<ResetPasswordOutcome> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hashHex = HashVerificationToken(request.Token.Trim());

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.PasswordResetToken != null && u.PasswordResetToken == hashHex,
                cancellationToken);

        if (user is null || !TokenHashesEqual(user.PasswordResetToken, hashHex))
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

        var passwordPolicy = await ValidatePasswordAgainstIdentityAsync(user, request.NewPassword, cancellationToken);
        if (!passwordPolicy.Succeeded)
        {
            return new ResetPasswordPasswordPolicyFailed(MapIdentityPasswordErrors(passwordPolicy));
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.PasswordResetUsed = true;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiryUtc = null;
        user.SecurityStamp = Guid.NewGuid().ToString();

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

    public async Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (user.OrganizationId == Guid.Empty)
        {
            return new UserProfileResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty,
                Array.Empty<string>(),
                Guid.Empty,
                string.Empty,
                string.Empty);
        }

        var roles = await userManager.GetRolesAsync(user);
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(
            o => o.Id == user.OrganizationId,
            cancellationToken);

        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            roles.ToArray(),
            user.OrganizationId,
            organization?.Name ?? string.Empty,
            organization?.JoinCode ?? string.Empty);
    }

    private async Task<IdentityResult> ValidatePasswordAgainstIdentityAsync(
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

    private static IReadOnlyDictionary<string, string[]> MapIdentityPasswordErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(e => e.Description).ToArray();
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ResetPasswordRequest.NewPassword)] = messages,
        };
    }

    private static string HashVerificationToken(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    private static bool TokenHashesEqual(string? storedHex, string computedHex)
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

    private static RegisterFailed ToRegisterFailure(IdentityResult result)
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
}
