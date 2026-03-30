using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskByIdHandler(
    TaskFlowDbContext dbContext,
    IMapper mapper) : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    public async Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        return task is null ? null : mapper.Map<TaskDto>(task);
    }
}

