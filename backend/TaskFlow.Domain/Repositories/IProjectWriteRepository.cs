namespace TaskFlow.Domain.Repositories;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

public interface IProjectWriteRepository
{
    Task<CreateProjectMutationResult> CreateProjectAsync(
        string name,
        string? description,
        CancellationToken cancellationToken);

    Task<UpdateProjectMutationResult?> UpdateProjectAsync(
        Guid projectId,
        string name,
        string? description,
        CancellationToken cancellationToken);

    Task<DeleteProjectMutationResult?> SoftDeleteProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<RestoreProjectMutationResult?> RestoreProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<MilestoneMutationResult?> CreateMilestoneAsync(
        Guid projectId,
        string name,
        string? description,
        DateTime? dueDateUtc,
        CancellationToken cancellationToken);

    Task<MilestoneMutationResult?> UpdateMilestoneAsync(
        Guid projectId,
        Guid milestoneId,
        string name,
        string? description,
        DateTime? dueDateUtc,
        CancellationToken cancellationToken);

    Task<MilestoneMutationResult?> DeleteMilestoneAsync(
        Guid projectId,
        Guid milestoneId,
        CancellationToken cancellationToken);

    Task<MoveProjectBoardTaskMutationResult?> MoveProjectBoardTaskAsync(
        Guid projectId,
        Guid taskId,
        DomainTaskStatus newStatus,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
