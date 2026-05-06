namespace TaskFlow.Domain.Repositories;

public interface ITaskBulkRepository
{
    Task<BulkTaskDeleteMutationResult> BulkSoftDeleteTasksAsync(
        IReadOnlyList<Guid> taskIds,
        CancellationToken cancellationToken);

    Task<BulkTaskUpdateMutationResult> BulkAssignTasksAsync(
        IReadOnlyList<Guid> taskIds,
        Guid? assigneeId,
        CancellationToken cancellationToken);

    Task<BulkTaskUpdateMutationResult> BulkUpdateTasksAsync(
        BulkTaskUpdateMutationInput input,
        CancellationToken cancellationToken);
}
