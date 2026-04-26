namespace TaskFlow.Application.Notifications;

public interface INotificationService
{
    Task CreateAsync(
        Guid userId,
        string type,
        string title,
        string body,
        string? entityType = null,
        Guid? entityId = null,
        bool sendEmail = false,
        string? toEmail = null,
        string? emailSubject = null,
        string? emailHtml = null,
        CancellationToken ct = default);
}
