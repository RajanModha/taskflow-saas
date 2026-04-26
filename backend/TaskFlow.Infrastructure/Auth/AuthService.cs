using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
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
    IOptions<JwtSettings> jwtSettings,
    IUserSessionIssuer sessionIssuer,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger) : IAuthService
{
    private const string ForgotPasswordResponseMessage =
        "If that email is registered you'll receive a link shortly.";

    private readonly EmailSettings _emailSettings = emailSettings.Value;
    private readonly JwtSettings _jwt = jwtSettings.Value;

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
            WorkspaceRole = TaskFlow.Domain.Entities.WorkspaceRole.Owner,
            WorkspaceJoinedAtUtc = now,
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
            response = await sessionIssuer.IssueSessionAsync(user, GetSessionConnectionInfo(), cancellationToken);
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
        static Task DelayOnFailureAsync(CancellationToken ct) => Task.Delay(Random.Shared.Next(50, 200), ct);

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
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
            response = await sessionIssuer.IssueSessionAsync(user, GetSessionConnectionInfo(), cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await DelayOnFailureAsync(cancellationToken);
            return new LoginFailed("User has not been assigned to a workspace.");
        }

        return new LoginSucceeded(response);
    }

    public async Task<RefreshSessionOutcome> RefreshSessionAsync(
        RefreshSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 3;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await RefreshSessionOnceAsync(request, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts - 1 && IsPostgresTransientConcurrency(ex))
            {
                dbContext.ChangeTracker.Clear();
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var stored = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hashHex, cancellationToken);

            if (stored is null || !TokenHashesEqual(stored.TokenHash, hashHex))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized);
            }

            var user = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == stored.UserId, cancellationToken);
            if (user is null || !user.EmailVerified || user.OrganizationId == Guid.Empty)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized);
            }

            if (stored.RevokedAtUtc.HasValue)
            {
                await RevokeAllActiveRefreshTokensForUserAsync(user.Id, now, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new RefreshSessionReuseDetected();
            }

            if (stored.ExpiresAtUtc <= now)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized);
            }

            var (rawNew, hashNew) = RefreshTokenCrypto.GenerateToken();
            var refreshDays = _jwt.RefreshTokenDays <= 0 ? 30 : _jwt.RefreshTokenDays;
            var newExpiryUtc = now.AddDays(refreshDays);

            stored.RevokedAtUtc = now;
            stored.ReplacedByTokenHash = hashNew;

            AuthResponse response;
            try
            {
                response = await sessionIssuer.AttachRefreshSessionAsync(
                    user,
                    rawNew,
                    hashNew,
                    newExpiryUtc,
                    GetSessionConnectionInfo(),
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new RefreshSessionFailed(
                    "Invalid refresh token",
                    "The refresh token is invalid or has been revoked.",
                    StatusCodes.Status401Unauthorized);
            }

            await transaction.CommitAsync(cancellationToken);
            return new RefreshSessionSucceeded(response);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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

    public async Task LogoutAsync(Guid userId, LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = RefreshTokenCrypto.HashRaw(request.RefreshToken.Trim());
        await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenHash == hash && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, now),
                cancellationToken);
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await RevokeAllActiveRefreshTokensForUserAsync(userId, now, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSessionItemDto>> GetSessionsAsync(
        Guid userId,
        string? refreshTokenRawForCurrentMarker,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        string? currentHash = null;
        if (!string.IsNullOrWhiteSpace(refreshTokenRawForCurrentMarker))
        {
            currentHash = RefreshTokenCrypto.HashRaw(refreshTokenRawForCurrentMarker.Trim());
        }

        var rows = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.DeviceInfo,
                t.IpAddress,
                t.CreatedAtUtc,
                t.ExpiresAtUtc,
                t.TokenHash,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(t => new UserSessionItemDto(
                t.Id,
                t.DeviceInfo,
                t.IpAddress,
                new DateTimeOffset(t.CreatedAtUtc, TimeSpan.Zero),
                new DateTimeOffset(t.ExpiresAtUtc, TimeSpan.Zero),
                currentHash is not null && TokenHashesEqual(t.TokenHash, currentHash)))
            .ToList();
    }

    public async Task<bool> TryRevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var affected = await dbContext.RefreshTokens
            .Where(t => t.Id == sessionId && t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, now),
                cancellationToken);
        return affected > 0;
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
        user.SecurityStamp = Guid.NewGuid().ToString();

        await RevokeAllActiveRefreshTokensForUserAsync(user.Id, now, cancellationToken);

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

        return await MapToProfileResponseAsync(user, cancellationToken);
    }

    public async Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return new ChangePasswordServerError();
        }

        var refreshHash = RefreshTokenCrypto.HashRaw(request.RefreshToken.Trim());
        var sessionValid = await dbContext.RefreshTokens
            .AsNoTracking()
            .AnyAsync(
                t => t.UserId == userId &&
                     t.TokenHash == refreshHash &&
                     t.RevokedAtUtc == null &&
                     t.ExpiresAtUtc > now,
                cancellationToken);
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var changeResult = await userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);
            if (!changeResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ChangePasswordPasswordPolicyFailed(MapChangePasswordIdentityErrors(changeResult));
            }

            await dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.TokenHash != refreshHash)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(t => t.RevokedAtUtc, now),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new ChangePasswordSucceeded("Password updated. Other devices have been logged out.");
    }

    public async Task<UpdateProfileOutcome> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
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
                var takenInOrg = await dbContext.Users
                    .IgnoreQueryFilters()
                    .AnyAsync(
                        u => u.OrganizationId == user.OrganizationId &&
                             u.Id != userId &&
                             u.NormalizedUserName == normalized,
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

        var refreshed = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(u => u.Id == userId, cancellationToken);
        var profile = await MapToProfileResponseAsync(refreshed, cancellationToken);
        return new UpdateProfileSucceeded(profile);
    }

    private async Task<UserProfileResponse> MapToProfileResponseAsync(
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
        var organization = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == user.OrganizationId, cancellationToken);

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

    private static string PickPrimaryRole(IReadOnlyList<string> roles)
    {
        foreach (var r in roles)
        {
            if (string.Equals(r, DomainRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return DomainRoles.Admin;
            }
        }

        return roles.Count > 0 ? roles[0] : DomainRoles.User;
    }

    private static IReadOnlyDictionary<string, string[]> MapChangePasswordIdentityErrors(IdentityResult result)
    {
        var messages = result.Errors.Select(e => e.Description).ToArray();
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ChangePasswordRequest.NewPassword)] = messages,
        };
    }

    private SessionConnectionInfo? GetSessionConnectionInfo()
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

    private Task RevokeAllActiveRefreshTokensForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.RevokedAtUtc, utcNow),
                cancellationToken);

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
