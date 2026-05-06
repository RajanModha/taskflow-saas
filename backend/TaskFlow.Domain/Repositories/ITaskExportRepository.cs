using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Domain.Repositories;

public interface ITaskExportRepository
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
}
