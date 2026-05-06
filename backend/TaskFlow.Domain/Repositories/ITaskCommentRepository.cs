namespace TaskFlow.Domain.Repositories;

public interface ITaskCommentRepository
{
    Task<TaskCommentMutationResult> CreateTaskCommentAsync(
        Guid taskId,
        Guid? authorId,
        string content,
        CancellationToken cancellationToken);

    Task<TaskCommentMutationResult> UpdateTaskCommentAsync(
        Guid taskId,
        Guid commentId,
        Guid? actorUserId,
        string content,
        CancellationToken cancellationToken);

    Task<TaskCommentMutationResult> DeleteTaskCommentAsync(
        Guid taskId,
        Guid commentId,
        Guid? actorUserId,
        CancellationToken cancellationToken);
}
