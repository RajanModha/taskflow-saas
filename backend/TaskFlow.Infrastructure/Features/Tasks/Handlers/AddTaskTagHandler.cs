using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class AddTaskTagHandler(
    ITaskRepository taskRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger)
    : IRequestHandler<AddTaskTagCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(AddTaskTagCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.AddTaskTagAsync(request.TaskId, request.TagId, cancellationToken);
        if (!result.TaskFound || !result.TagFound)
        {
            return null;
        }

        if (result.Changed && currentUser.UserId is { } actorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                result.TaskId,
                ActivityActions.TaskTagAdded,
                actorId,
                string.Empty,
                result.OrganizationId,
                new { tagId = request.TagId, tagName = string.Empty },
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, result.AssigneeId);
        boardCacheVersion.BumpProject(result.ProjectId);

        var detached = await taskRepository.GetDetachedTaskByIdAsync(result.TaskId, cancellationToken);
        if (detached is null)
        {
            return null;
        }
        var dtoList = await taskReadModelAssembler.ToTaskDtosAsync([detached], cancellationToken);
        return dtoList.Count == 0 ? null : dtoList[0];
    }
}
