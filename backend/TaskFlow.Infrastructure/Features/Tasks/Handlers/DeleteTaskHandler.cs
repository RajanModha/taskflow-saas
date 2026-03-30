using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class DeleteTaskHandler(
    TaskFlowDbContext dbContext,
    IMemoryCache cache) : IRequestHandler<DeleteTaskCommand, bool>
{
    public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return false;
        }

        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        cache.Remove(DashboardCacheKeys.DashboardStats(task.OrganizationId));
        return true;
    }
}

