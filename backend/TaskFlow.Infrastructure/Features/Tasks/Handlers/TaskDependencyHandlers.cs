using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Persistence;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class AddTaskDependencyCommandHandler(
    ITaskDependencyRepository taskRepository,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<AddTaskDependencyCommand, AddTaskDependencyResult>
{
    public async Task<AddTaskDependencyResult> Handle(AddTaskDependencyCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.AddTaskDependencyAsync(request.TaskId, request.BlockingTaskId, cancellationToken);
        switch (result.Outcome)
        {
            case "self":
                return new AddTaskDependencyResult.SelfReference();
            case "not_found":
                return new AddTaskDependencyResult.NotFound();
            case "duplicate":
                return new AddTaskDependencyResult.Duplicate();
            case "max":
                return new AddTaskDependencyResult.MaxDependencies();
            case "cycle":
                return new AddTaskDependencyResult.Cycle();
            case "ok":
                if (result.BlockedProjectId is { } blockedProjectId)
                {
                    boardCacheVersion.BumpProject(blockedProjectId);
                }
                if (result.BlockingProjectId is { } blockingProjectId && result.BlockingProjectId != result.BlockedProjectId)
                {
                    boardCacheVersion.BumpProject(blockingProjectId);
                }
                return new AddTaskDependencyResult.Ok(
                    new DependencyDto(
                        request.TaskId,
                        new TaskBlockingSummaryDto(
                            result.BlockingTaskId ?? Guid.Empty,
                            result.BlockingTaskTitle ?? string.Empty,
                            result.BlockingTaskStatus ?? DomainTaskStatus.Backlog)));
            default:
                return new AddTaskDependencyResult.NotFound();
        }
    }
}

public sealed class RemoveTaskDependencyCommandHandler(
    ITaskDependencyRepository taskRepository,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<RemoveTaskDependencyCommand, bool>
{
    public async Task<bool> Handle(RemoveTaskDependencyCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.RemoveTaskDependencyAsync(request.TaskId, request.BlockingTaskId, cancellationToken);
        var deleted = result.Deleted;
        if (deleted)
        {
            if (result.BlockedProjectId is { } blockedProjectId)
            {
                boardCacheVersion.BumpProject(blockedProjectId);
            }
            if (result.BlockingProjectId is { } blockingProjectId && result.BlockingProjectId != result.BlockedProjectId)
            {
                boardCacheVersion.BumpProject(blockingProjectId);
            }
        }

        return deleted;
    }
}

public sealed class GetTaskDependenciesQueryHandler(ITaskReadRepository taskRepository)
    : IRequestHandler<GetTaskDependenciesQuery, TaskDependenciesResponse?>
{
    public async Task<TaskDependenciesResponse?> Handle(GetTaskDependenciesQuery request, CancellationToken cancellationToken)
    {
        var me = await taskRepository.GetTaskDependenciesAsync(request.TaskId, cancellationToken);
        if (me is null)
        {
            return null;
        }

        var blockedBy = me.BlockedBy
            .Select(b => new DependencyDto(
                me.TaskId,
                new TaskBlockingSummaryDto(b.Id, b.Title, b.Status)))
            .ToList();

        var blocking = me.Blocking
            .Select(b =>
            {
                return new DependencyDto(
                    b.Id,
                    new TaskBlockingSummaryDto(me.TaskId, me.Title, me.Status));
            })
            .ToList();

        return new TaskDependenciesResponse(blockedBy, blocking);
    }
}
