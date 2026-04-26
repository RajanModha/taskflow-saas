namespace TaskFlow.Domain.Entities;

public sealed class ActivityLog
{
    public Guid Id { get; init; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public Guid ActorId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public string? Metadata { get; set; }

    public Guid OrganizationId { get; set; }
}
