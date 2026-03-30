namespace TaskFlow.Domain.Entities;

/// <summary>
/// Workspace / tenant entity.
/// </summary>
public sealed class Organization
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string JoinCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

