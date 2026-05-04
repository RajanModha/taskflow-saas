using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Application.Tasks;

/// <summary>Builds <see cref="TaskDto"/> graphs from detached domain tasks (Infrastructure uses EF for related lookups).</summary>
public interface ITaskReadModelAssembler
{
    System.Threading.Tasks.Task<IReadOnlyList<TaskDto>> ToTaskDtosAsync(
        IReadOnlyList<DomainTask> tasks,
        CancellationToken cancellationToken);
}
