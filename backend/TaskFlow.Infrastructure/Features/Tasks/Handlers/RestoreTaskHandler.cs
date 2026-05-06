using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class RestoreTaskHandler(
    ITaskRepository taskRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<RestoreTaskCommand, TaskDto?>
{
    public async Task<TaskDto?> Handle(RestoreTaskCommand request, CancellationToken cancellationToken)
    {
        var restored = await taskRepository.RestoreTaskAsync(request.TaskId, cancellationToken);
        if (restored is null)
        {
            return null;
        }

        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            restored.OrganizationId,
            currentUser.UserId,
            null,
            restored.AssigneeId);
        boardCacheVersion.BumpProject(restored.ProjectId);

        var detached = await taskRepository.GetDetachedTaskByIdAsync(restored.TaskId, cancellationToken);
        if (detached is null)
        {
            return null;
        }

        var dto = await taskReadModelAssembler.ToTaskDtosAsync([detached], cancellationToken);
        return dto.Count == 0 ? null : dto[0];
    }
}
