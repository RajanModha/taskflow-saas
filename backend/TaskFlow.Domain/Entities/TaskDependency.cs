namespace TaskFlow.Domain.Entities;

/// <summary>
/// <see cref="BlockedTaskId"/> cannot proceed until <see cref="BlockingTaskId"/> is Done or Cancelled.
/// </summary>
public sealed class TaskDependency
{
    public Guid BlockedTaskId { get; set; }
    public Guid BlockingTaskId { get; set; }

    public Task BlockedTask { get; set; } = null!;
    public Task BlockingTask { get; set; } = null!;
}
