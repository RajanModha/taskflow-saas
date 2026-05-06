using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Repositories;

public interface IProjectReadRepository
{
    Task<PagedResult<ProjectReadModel>> GetPagedProjectsAsync(
        ProjectListCriteria criteria,
        CancellationToken cancellationToken);

    Task<ProjectReadModel?> GetProjectByIdAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<PagedResult<ProjectActivityRow>?> GetProjectActivityAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MilestoneReadModel>?> GetProjectMilestonesAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, (int Total, int Completed)>> GetMilestoneStatsAsync(
        IReadOnlyList<Guid> milestoneIds,
        CancellationToken cancellationToken);

    Task<ProjectBoardTaskReadModel?> GetBoardTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    Task<(ProjectReadModel Project, IReadOnlyList<ProjectBoardTaskReadModel> Tasks)?> GetProjectBoardDataAsync(
        Guid projectId,
        Guid? assigneeId,
        Guid? tagId,
        string? q,
        CancellationToken cancellationToken);

    Task<(ProjectReadModel Project, IReadOnlyList<TaskFlow.Domain.Entities.Task> Tasks)?> GetProjectExportDataAsync(
        Guid projectId,
        CancellationToken cancellationToken);
}
