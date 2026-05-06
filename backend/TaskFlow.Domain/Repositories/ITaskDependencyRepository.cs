namespace TaskFlow.Domain.Repositories;

public interface ITaskDependencyRepository
{
    Task<TaskDependencyAddMutationResult> AddTaskDependencyAsync(
        Guid taskId,
        Guid blockingTaskId,
        CancellationToken cancellationToken);

    Task<TaskDependencyRemoveMutationResult> RemoveTaskDependencyAsync(
        Guid taskId,
        Guid blockingTaskId,
        CancellationToken cancellationToken);
}
