using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Auth;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
