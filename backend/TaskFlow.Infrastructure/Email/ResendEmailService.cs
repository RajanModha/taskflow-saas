using Microsoft.Extensions.Logging;
using Resend;

namespace TaskFlow.Infrastructure.Email;

public sealed class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        IResend resend,
        ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string? emailOperation = null,
        CancellationToken ct = default)
    {
        var operation = emailOperation ?? "Unspecified";
        try
        {
            var message = new EmailMessage
            {
                From = "TaskFlow <onboarding@resend.dev>",
                Subject = subject,
                HtmlBody = htmlBody,
            };
            message.To.Add(string.IsNullOrWhiteSpace(toName) ? toEmail : $"{toName} <{toEmail}>");

            var response = await _resend.EmailSendAsync(message, ct);
            _logger.LogInformation(
                "Email sent. Operation={EmailOperation} Recipient={Recipient} Subject={Subject} ResendId={ResendId}",
                operation,
                toEmail,
                subject,
                response?.Content);
        }
        catch (Exception ex)
        {
            // Never crash the API on email failure.
            _logger.LogError(
                ex,
                "Email send failed. Operation={EmailOperation} Recipient={Recipient} Subject={Subject}",
                operation,
                toEmail,
                subject);
        }
    }
}

