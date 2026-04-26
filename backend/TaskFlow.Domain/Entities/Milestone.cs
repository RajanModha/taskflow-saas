namespace TaskFlow.Domain.Entities;

public sealed class Milestone : ISoftDeletable
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ProjectId { get; init; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Project? Project { get; set; }
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
}
