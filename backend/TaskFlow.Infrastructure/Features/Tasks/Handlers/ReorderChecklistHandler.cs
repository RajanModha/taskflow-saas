using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class ReorderChecklistHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    IMemoryCache cache)
    : IRequestHandler<ReorderChecklistCommand, IReadOnlyList<ChecklistItemDto>?>
{
    public async Task<IReadOnlyList<ChecklistItemDto>?> Handle(ReorderChecklistCommand request, CancellationToken cancellationToken)
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

        var items = await dbContext.ChecklistItems
            .Where(c => c.TaskId == request.TaskId)
            .ToListAsync(cancellationToken);

        if (items.Count != request.OrderedIds.Length)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(request.OrderedIds),
                    "Must include every checklist item exactly once."),
            ]);
        }

        var existingIds = items.Select(i => i.Id).OrderBy(id => id).ToList();
        var requestedSorted = request.OrderedIds.OrderBy(id => id).ToList();
        if (!existingIds.SequenceEqual(requestedSorted))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(request.OrderedIds),
                    "Must include every checklist item exactly once."),
            ]);
        }

        for (var i = 0; i < request.OrderedIds.Length; i++)
        {
            var id = request.OrderedIds[i];
            var item = items.First(x => x.Id == id);
            item.Order = i + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        boardCacheVersion.BumpProject(task.ProjectId);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, task.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, task.AssigneeId);

        var ordered = request.OrderedIds
            .Select(id => items.First(x => x.Id == id))
            .Select(ChecklistItemMapper.ToDto)
            .ToList();

        return ordered;
    }
}
