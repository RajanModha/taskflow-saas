using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class AddChecklistItemHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    IBoardCacheVersion boardCacheVersion,
    TimeProvider timeProvider)
    : IRequestHandler<AddChecklistItemCommand, ChecklistItemDto?>
{
    public async Task<ChecklistItemDto?> Handle(AddChecklistItemCommand request, CancellationToken cancellationToken)
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

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var items = await dbContext.ChecklistItems
            .Where(c => c.TaskId == request.TaskId)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);

        var title = request.Title.Trim();
        var maxOrder = items.Count == 0 ? 0 : items.Max(i => i.Order);
        int newOrder;
        if (request.InsertAfterOrder is null)
        {
            newOrder = maxOrder + 1;
        }
        else
        {
            var after = request.InsertAfterOrder.Value;
            if (maxOrder == 0 || after >= maxOrder)
            {
                newOrder = maxOrder + 1;
            }
            else if (after <= 0)
            {
                foreach (var i in items)
                {
                    i.Order += 1;
                }

                newOrder = 1;
            }
            else
            {
                foreach (var i in items.Where(x => x.Order > after))
                {
                    i.Order += 1;
                }

                newOrder = after + 1;
            }
        }

        var entity = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            Title = title,
            IsCompleted = false,
            Order = newOrder,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            CompletedAtUtc = null,
        };

        await dbContext.ChecklistItems.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        boardCacheVersion.BumpProject(task.ProjectId);
        return ChecklistItemMapper.ToDto(entity);
    }
}
