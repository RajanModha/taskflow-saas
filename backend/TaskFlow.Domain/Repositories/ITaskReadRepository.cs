using TaskFlow.Domain.Common;
using DomainTask = TaskFlow.Domain.Entities.Task;
using ActivityLog = TaskFlow.Domain.Entities.ActivityLog;

namespace TaskFlow.Domain.Repositories;

public interface ITaskReadRepository
{
    Task<PagedResult<DomainTask>> GetPagedTasksAsync(
        TaskListCriteria criteria,
        CancellationToken cancellationToken);

    Task<DomainTask?> GetDetachedTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    Task<PagedResult<DomainTask>> GetPagedOverdueTasksAsync(
        OverdueTaskListCriteria criteria,
        CancellationToken cancellationToken);

    Task<PagedResult<TaskCommentReadModel>?> GetPagedTaskCommentsAsync(
        Guid taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskChecklistItemReadModel>?> GetTaskChecklistAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    Task<PagedResult<ActivityLog>?> GetPagedTaskActivityAsync(
        Guid taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<TaskDependenciesReadModel?> GetTaskDependenciesAsync(
        Guid taskId,
        CancellationToken cancellationToken);
}
