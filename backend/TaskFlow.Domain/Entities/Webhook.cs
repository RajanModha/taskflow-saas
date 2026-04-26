namespace TaskFlow.Domain.Entities;

public sealed class Webhook
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Url { get; set; } = string.Empty;
    /// <summary>At-rest protected secret (not plaintext in DB).</summary>
    public string Secret { get; set; } = string.Empty;
    public string Events { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; init; }
    public Guid CreatedByUserId { get; init; }

    public Organization? Organization { get; set; }
    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}
