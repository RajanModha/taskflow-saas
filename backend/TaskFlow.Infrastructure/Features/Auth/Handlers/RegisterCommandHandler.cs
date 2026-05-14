using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
