using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tenancy;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks;

internal static class TaskTenantGuard
{
    public static async System.Threading.Tasks.Task<DomainTask?> GetTaskInCurrentTenantAsync(
        TaskFlowDbContext dbContext,
        ICurrentTenant currentTenant,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            return null;
        }

        return await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == taskId && t.OrganizationId == currentTenant.OrganizationId,
                cancellationToken);
    }

    public static async System.Threading.Tasks.Task<DomainTask?> GetTaskForMutationAsync(
        TaskFlowDbContext dbContext,
        ICurrentTenant currentTenant,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            return null;
        }

        return await dbContext.Tasks
            .FirstOrDefaultAsync(
                t => t.Id == taskId && t.OrganizationId == currentTenant.OrganizationId,
                cancellationToken);
    }
}
