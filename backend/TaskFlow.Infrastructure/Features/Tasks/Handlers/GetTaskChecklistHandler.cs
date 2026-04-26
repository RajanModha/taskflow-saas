using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskChecklistHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant)
    : IRequestHandler<GetTaskChecklistQuery, IReadOnlyList<ChecklistItemDto>?>
{
    public async Task<IReadOnlyList<ChecklistItemDto>?> Handle(
        GetTaskChecklistQuery request,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(
            dbContext,
            currentTenant,
            request.TaskId,
            cancellationToken);

        if (task is null)
        {
            return null;
        }

        var rows = await dbContext.ChecklistItems
            .AsNoTracking()
            .Where(c => c.TaskId == request.TaskId)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);

        return rows.Select(ChecklistItemMapper.ToDto).ToList();
    }
}
