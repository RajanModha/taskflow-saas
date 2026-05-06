using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class ReorderChecklistHandler(
    ITaskChecklistRepository taskRepository,
    ITaskReadRepository taskReadRepository,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    IMemoryCache cache)
    : IRequestHandler<ReorderChecklistCommand, IReadOnlyList<ChecklistItemDto>?>
{
    public async Task<IReadOnlyList<ChecklistItemDto>?> Handle(ReorderChecklistCommand request, CancellationToken cancellationToken)
    {
        var ordered = await taskRepository.ReorderChecklistAsync(
            request.TaskId,
            request.OrderedIds,
            cancellationToken);
        if (ordered is null)
        {
            return null;
        }
        var detached = await taskReadRepository.GetDetachedTaskByIdAsync(request.TaskId, cancellationToken);
        if (detached is null)
        {
            return null;
        }
        boardCacheVersion.BumpProject(detached.ProjectId);
        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, detached.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, detached.AssigneeId);
        return ordered
            .Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsCompleted, i.Order, i.CompletedAtUtc))
            .ToList();
    }
}
