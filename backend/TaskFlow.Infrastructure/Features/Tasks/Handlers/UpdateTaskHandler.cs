using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class UpdateTaskHandler(
    TaskFlowDbContext dbContext,
    IMapper mapper,
    IMemoryCache cache)
    : IRequestHandler<UpdateTaskCommand, TaskDto?>
{
    public async Task<TaskDto?> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDateUtc = request.DueDateUtc;
        task.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        cache.Remove(DashboardCacheKeys.DashboardStats(task.OrganizationId));
        return mapper.Map<TaskDto>(task);
    }
}

