using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tenancy;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class WorkspaceAccessRepository(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant) : IWorkspaceAccessRepository
{
    public async Task<WorkspaceActorContext?> GetActorInCurrentTenantAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            return null;
        }

        var actor = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == userId && u.OrganizationId == currentTenant.OrganizationId)
            .Select(u => new { u.Id, u.OrganizationId, u.WorkspaceRole })
            .FirstOrDefaultAsync(cancellationToken);
        if (actor is null || actor.OrganizationId == Guid.Empty)
        {
            return null;
        }

        return new WorkspaceActorContext(actor.Id, actor.OrganizationId, actor.WorkspaceRole);
    }
}
