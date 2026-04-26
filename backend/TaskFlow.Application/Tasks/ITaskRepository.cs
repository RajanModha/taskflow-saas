using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Application.Tasks;

public interface ITaskRepository
{
    Task<long> GetExportCountAsync(TaskExportFilters filters, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetExportAssigneeDisplayNamesAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken);

    IAsyncEnumerable<DomainTask> GetExportStreamAsync(
        TaskExportFilters filters,
        CancellationToken cancellationToken);
}
