using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class AddChecklistItemHandler(
    ITaskChecklistRepository taskRepository,
    ICurrentUser currentUser,
    IBoardCacheVersion boardCacheVersion,
    IMemoryCache cache)
    : IRequestHandler<AddChecklistItemCommand, ChecklistItemDto?>
{
    public async Task<ChecklistItemDto?> Handle(AddChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.AddChecklistItemAsync(
            request.TaskId,
            request.Title,
            request.InsertAfterOrder,
            cancellationToken);
        if (result is null)
        {
            return null;
        }
        boardCacheVersion.BumpProject(result.ProjectId);
        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, result.AssigneeId);
        return new ChecklistItemDto(
            result.Item.Id,
            result.Item.Title,
            result.Item.IsCompleted,
            result.Item.Order,
            result.Item.CompletedAtUtc);
    }
}
