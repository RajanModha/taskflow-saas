using TaskFlow.Domain.Common;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Domain.Repositories;

/// <summary>
/// Persistence port for task reads (export and list). Implementations use EF; tenant isolation relies on
/// ambient tenant query filters on the persistence context unless a method documents an explicit organization check.
/// </summary>
public interface ITaskRepository
{
    System.Threading.Tasks.Task<long> GetExportCountAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken);

    System.Threading.Tasks.Task<IReadOnlyDictionary<Guid, string>> GetExportAssigneeDisplayNamesAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken);

    IAsyncEnumerable<DomainTask> GetExportStreamAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken);

    /// <summary>Returns no-tracking task rows; application layer maps them to API DTOs.</summary>
    System.Threading.Tasks.Task<PagedResult<DomainTask>> GetPagedTasksAsync(
        TaskListCriteria criteria,
        CancellationToken cancellationToken);

    /// <summary>Loads a single no-tracking task in the current organization, or null if missing or tenant is not set.</summary>
    System.Threading.Tasks.Task<DomainTask?> GetDetachedTaskByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    /// <summary>Due before UTC now, excluding completed/cancelled; respects soft-delete query filters.</summary>
    System.Threading.Tasks.Task<PagedResult<DomainTask>> GetPagedOverdueTasksAsync(
        OverdueTaskListCriteria criteria,
        CancellationToken cancellationToken);
}
