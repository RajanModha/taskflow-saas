namespace TaskFlow.Domain.Entities;

public sealed class TaskTag
{
    public Guid TaskId { get; set; }

    public Guid TagId { get; set; }

    public Task ParentTask { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}
