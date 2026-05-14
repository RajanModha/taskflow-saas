using System.Data;
using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Auth;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Features.Auth.Handlers;

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
