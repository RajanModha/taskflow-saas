using MediatR;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskByIdHandler(
    ITaskRepository taskRepository,
    ITaskReadModelAssembler taskReadModelAssembler) : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetDetachedTaskByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var dtos = await taskReadModelAssembler.ToTaskDtosAsync([task], cancellationToken);
        return dtos.Count > 0 ? dtos[0] : null;
    }
}
