namespace TaskFlow.Domain.Entities;

public sealed class ChecklistItem
{
    public Guid Id { get; init; }

    public Guid TaskId { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    /// <summary>1-based display order within the task.</summary>
    public int Order { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public Task ParentTask { get; set; } = null!;
}
