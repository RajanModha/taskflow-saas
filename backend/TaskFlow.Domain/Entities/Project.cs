namespace TaskFlow.Domain.Entities;

public sealed class Project : ISoftDeletable
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

