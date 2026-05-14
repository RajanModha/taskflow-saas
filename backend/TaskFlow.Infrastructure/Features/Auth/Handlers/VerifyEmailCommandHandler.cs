using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
