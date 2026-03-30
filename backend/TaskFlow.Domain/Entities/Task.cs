namespace TaskFlow.Domain.Entities;

public enum TaskStatus
{
    Backlog = 0,
    Todo = 1,
    InProgress = 2,
    Done = 3,
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3,
}

public sealed class Task
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }
    public Guid ProjectId { get; init; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }

    // Navigation optional; EF can manage with FK if needed.
    public Project? Project { get; set; }
}

