namespace TaskFlow.Domain.Repositories;

public interface ITaskChecklistRepository
{
    Task<ChecklistMutationResult?> AddChecklistItemAsync(
        Guid taskId,
        string title,
        int? insertAfterOrder,
        CancellationToken cancellationToken);

    Task<ChecklistMutationResult?> UpdateChecklistItemAsync(
        Guid taskId,
        Guid itemId,
        string? title,
        bool? isCompleted,
        CancellationToken cancellationToken);

    Task<ChecklistDeleteMutationResult?> DeleteChecklistItemAsync(
        Guid taskId,
        Guid itemId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskChecklistItemReadModel>?> ReorderChecklistAsync(
        Guid taskId,
        IReadOnlyList<Guid> orderedIds,
        CancellationToken cancellationToken);
}
