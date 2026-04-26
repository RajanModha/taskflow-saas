namespace TaskFlow.Domain.Entities;

public sealed class WebhookDelivery
{
    public Guid Id { get; init; }
    public Guid WebhookId { get; init; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = WebhookDeliveryStatuses.Pending;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public int? ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime CreatedAtUtc { get; init; }

    public Webhook? Webhook { get; set; }
}

public static class WebhookDeliveryStatuses
{
    public const string Pending = "Pending";
    public const string Success = "Success";
    public const string Failed = "Failed";
}
