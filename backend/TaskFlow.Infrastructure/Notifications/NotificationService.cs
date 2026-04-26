using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Notifications;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Notifications;

public sealed class NotificationService(
    TaskFlowDbContext dbContext,
    IMemoryCache cache,
    IEmailService emailService,
    ILogger<NotificationService> logger,
    TimeProvider timeProvider) : INotificationService
{
    public async Task CreateAsync(
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
        CancellationToken ct = default)
    {
        // Notifications may be created from flows where tenant context is transiently unavailable.
        // Bypass tenant query filters for this existence check and rely on explicit user targeting.
        var exists = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, ct);
        if (!exists)
        {
            return;
        }

        var notification = new TaskFlow.Domain.Entities.Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        await dbContext.Notifications.AddAsync(notification, ct);
        await dbContext.SaveChangesAsync(ct);

        cache.Remove(NotificationCacheKeys.UnreadCount(userId));

        if (sendEmail && !string.IsNullOrWhiteSpace(toEmail) &&
            !string.IsNullOrWhiteSpace(emailSubject) &&
            !string.IsNullOrWhiteSpace(emailHtml))
        {
            try
            {
                await emailService.SendEmailAsync(
                    toEmail,
                    toEmail,
                    emailSubject,
                    emailHtml,
                    "Notification",
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed sending notification email to {Email}", toEmail);
            }
        }
    }
}
