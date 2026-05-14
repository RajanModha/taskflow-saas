using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
