namespace TaskFlow.Domain.Entities;

public enum TaskStatus
{
    Backlog = 0,
    Todo = 1,
    InProgress = 2,
    Done = 3,
    Cancelled = 4,
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3,
}

public sealed class Task : ISoftDeletable
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

    public Guid? AssigneeId { get; set; }

    public Guid? MilestoneId { get; set; }

    public Guid? TemplateId { get; set; }

    public bool ReminderSent { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public uint RowVersion { get; set; }

    // Navigation optional; EF can manage with FK if needed.
    public Project? Project { get; set; }

    public Milestone? Milestone { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();

    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();

    public ICollection<TaskDependency> BlockedByDependencies { get; set; } = new List<TaskDependency>();

    public ICollection<TaskDependency> BlockingDependencies { get; set; } = new List<TaskDependency>();
}

