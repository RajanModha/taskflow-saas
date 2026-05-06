using TaskFlow.Domain.Common;
using DomainTask = TaskFlow.Domain.Entities.Task;
using System.Threading.Tasks;

namespace TaskFlow.Domain.Repositories;

/// <summary>
/// Persistence port for task reads (export and list). Implementations use EF; tenant isolation relies on
/// ambient tenant query filters on the persistence context unless a method documents an explicit organization check.
/// </summary>
public interface ITaskRepository
{
    Task<long> GetExportCountAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetExportAssigneeDisplayNamesAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken);

    IAsyncEnumerable<DomainTask> GetExportStreamAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken);

    /// <summary>Returns no-tracking task rows; application layer maps them to API DTOs.</summary>
    Task<PagedResult<DomainTask>> GetPagedTasksAsync(
        TaskListCriteria criteria,
        CancellationToken cancellationToken);

    /// <summary>Loads a single no-tracking task in the current organization, or null if missing or tenant is not set.</summary>
    Task<DomainTask?> GetDetachedTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    /// <summary>Due before UTC now, excluding completed/cancelled; respects soft-delete query filters.</summary>
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

    Task<PagedResult<TaskFlow.Domain.Entities.ActivityLog>?> GetPagedTaskActivityAsync(
        Guid taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<TaskDependenciesReadModel?> GetTaskDependenciesAsync(
        Guid taskId,
        CancellationToken cancellationToken);

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

    Task<TaskTagMutationResult> AddTaskTagAsync(
        Guid taskId,
        Guid tagId,
        CancellationToken cancellationToken);

    Task<TaskTagMutationResult> RemoveTaskTagAsync(
        Guid taskId,
        Guid tagId,
        CancellationToken cancellationToken);

    Task<TaskDependencyAddMutationResult> AddTaskDependencyAsync(
        Guid taskId,
        Guid blockingTaskId,
        CancellationToken cancellationToken);

    Task<TaskDependencyRemoveMutationResult> RemoveTaskDependencyAsync(
        Guid taskId,
        Guid blockingTaskId,
        CancellationToken cancellationToken);

    Task<CreateTaskMutationResult?> CreateTaskAsync(
        CreateTaskMutationInput input,
        CancellationToken cancellationToken);

    Task<CreateTaskMutationResult?> CreateTaskFromTemplateAsync(
        CreateTaskFromTemplateMutationInput input,
        CancellationToken cancellationToken);
}
