namespace TaskFlow.Infrastructure.Email;

public interface IEmailService
{
    /// <param name="emailOperation">Logical operation name for structured logs (e.g. RegisterVerify, PasswordReset).</param>
    Task SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string? emailOperation = null,
        CancellationToken ct = default);
}

