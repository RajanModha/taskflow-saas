using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskByIdHandler(TaskFlowDbContext dbContext)
    : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        var dtoList = await TaskProjection.ToDtosAsync(dbContext, [task], cancellationToken);
        return dtoList[0];
    }
}
