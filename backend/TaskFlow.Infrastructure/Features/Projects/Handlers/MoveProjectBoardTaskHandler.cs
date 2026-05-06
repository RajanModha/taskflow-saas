using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class MoveProjectBoardTaskHandler(
    IProjectWriteRepository projectWriteRepository,
    IProjectReadRepository projectReadRepository,
    ICurrentUser currentUser,
    ICurrentUserService currentUserService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    TimeProvider timeProvider,
    IActivityLogger activityLogger)
    : IRequestHandler<MoveProjectBoardTaskCommand, BoardTaskDto?>
{
    public async System.Threading.Tasks.Task<BoardTaskDto?> Handle(
        MoveProjectBoardTaskCommand request,
        CancellationToken cancellationToken)
    {
        var moved = await projectWriteRepository.MoveProjectBoardTaskAsync(
            request.ProjectId,
            request.TaskId,
            request.NewStatus,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
        if (moved is null || !moved.TaskFound)
        {
            return null;
        }

        if (!moved.StatusChanged)
        {
            return await LoadBoardTaskDtoAsync(moved.TaskId, cancellationToken);
        }

        if (currentUser.UserId is { } actorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                moved.TaskId,
                ActivityActions.TaskStatusChanged,
                actorId,
                currentUserService.UserName,
                moved.OrganizationId,
                new { from = moved.PreviousStatus.ToString(), to = request.NewStatus.ToString(), projectId = moved.ProjectId },
                cancellationToken);
        }

        boardCacheVersion.BumpProject(moved.ProjectId);
        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            moved.OrganizationId,
            currentUser.UserId,
            moved.AssigneeId,
            moved.AssigneeId);

        return await LoadBoardTaskDtoAsync(moved.TaskId, cancellationToken);
    }

    private async System.Threading.Tasks.Task<BoardTaskDto?> LoadBoardTaskDtoAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await projectReadRepository.GetBoardTaskByIdAsync(taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var assignee = task.AssigneeId is { } aid
            ? new TaskFlow.Application.Tasks.TaskAssigneeDto(aid, task.AssigneeUserName ?? string.Empty, task.AssigneeDisplayName)
            : null;
        var tags = task.Tags
            .Select(t => new TaskFlow.Application.Tasks.TagDto(t.Id, t.Name, t.Color))
            .ToList();
        return new BoardTaskDto(
            task.TaskId,
            task.Title,
            task.Priority,
            task.DueDateUtc,
            task.DueDateUtc.HasValue && task.DueDateUtc.Value < nowUtc,
            assignee,
            tags,
            task.CommentCount,
            task.CreatedAtUtc);
    }
}
