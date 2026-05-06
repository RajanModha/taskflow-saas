namespace TaskFlow.Domain.Repositories;

public interface ITaskWriteRepository
{
    Task<AssignTaskMutationResult?> AssignTaskAsync(
        Guid taskId,
        Guid? assigneeId,
        CancellationToken cancellationToken);

    Task<PatchTaskMutationResult?> PatchTaskAsync(
        PatchTaskMutationInput input,
        CancellationToken cancellationToken);

    Task<UpdateTaskMutationResult?> UpdateTaskAsync(
        UpdateTaskMutationInput input,
        CancellationToken cancellationToken);

    Task<DeleteTaskMutationResult?> SoftDeleteTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    Task<DeleteTaskMutationResult?> RestoreTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    Task<DeleteTaskMutationResult?> PermanentDeleteTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    Task<CreateTaskMutationResult?> CreateTaskAsync(
        CreateTaskMutationInput input,
        CancellationToken cancellationToken);

    Task<CreateTaskMutationResult?> CreateTaskFromTemplateAsync(
        CreateTaskFromTemplateMutationInput input,
        CancellationToken cancellationToken);
}
