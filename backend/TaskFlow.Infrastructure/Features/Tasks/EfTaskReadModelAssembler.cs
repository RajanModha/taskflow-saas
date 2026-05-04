using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks;

public sealed class EfTaskReadModelAssembler(TaskFlowDbContext dbContext) : ITaskReadModelAssembler
{
    public async System.Threading.Tasks.Task<IReadOnlyList<TaskDto>> ToTaskDtosAsync(
        IReadOnlyList<DomainTask> tasks,
        CancellationToken cancellationToken)
    {
        if (tasks.Count == 0)
        {
            return [];
        }

        return await TaskProjection.ToDtosAsync(dbContext, tasks, cancellationToken);
    }
}
