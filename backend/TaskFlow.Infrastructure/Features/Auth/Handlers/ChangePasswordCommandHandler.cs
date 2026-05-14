using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
