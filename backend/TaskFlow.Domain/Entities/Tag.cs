namespace TaskFlow.Domain.Entities;

public sealed class Tag
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}
