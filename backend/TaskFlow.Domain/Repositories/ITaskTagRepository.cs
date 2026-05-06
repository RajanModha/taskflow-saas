namespace TaskFlow.Domain.Repositories;

public interface ITaskTagRepository
{
    Task<TaskTagMutationResult> AddTaskTagAsync(
        Guid taskId,
        Guid tagId,
        CancellationToken cancellationToken);

    Task<TaskTagMutationResult> RemoveTaskTagAsync(
        Guid taskId,
        Guid tagId,
        CancellationToken cancellationToken);
}
