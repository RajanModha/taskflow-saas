using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class BulkUpdateTasksHandler(
    ITaskBulkRepository taskRepository,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger)
    : IRequestHandler<BulkUpdateTasksCommand, BulkTaskOperationResultDto>
{
    public async Task<BulkTaskOperationResultDto> Handle(BulkUpdateTasksCommand request, CancellationToken cancellationToken)
    {
        var result = await taskRepository.BulkUpdateTasksAsync(
            new BulkTaskUpdateMutationInput(
                request.TaskIds,
                request.Updates.Status,
                request.Updates.Priority,
                request.Updates.DueDateUtc,
                request.Updates.AssigneeId,
                request.Updates.HasDueDateUtc,
                request.Updates.HasAssigneeId),
            cancellationToken);

        var failures = result.NotFound
            .Select(id => new BulkTaskFailureDto(id, "not_found"))
            .ToList();
        if (result.InvalidAssignee)
        {
            return new BulkTaskOperationResultDto(0, [new BulkTaskFailureDto(Guid.Empty, "invalid_assignee")]);
        }

        if (currentUser.UserId is { } actorId)
        {
            foreach (var task in result.Mutated.Where(t => failures.All(f => f.TaskId != t.TaskId)))
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    task.TaskId,
                    ActivityActions.TaskBulkUpdated,
                    actorId,
                    string.Empty,
                    task.OrganizationId,
                    new { bulk = true },
                    cancellationToken);
            }
        }

        foreach (var task in result.Mutated.Where(t => failures.All(f => f.TaskId != t.TaskId)))
        {
            DashboardCacheInvalidation.InvalidateAfterTaskMutation(
                cache,
                task.OrganizationId,
                currentUser.UserId,
                task.PreviousAssigneeId,
                task.CurrentAssigneeId);
            boardCacheVersion.BumpProject(task.ProjectId);
        }

        var succeeded = result.Mutated.Count - failures.Count(f => f.TaskId != Guid.Empty);
        return new BulkTaskOperationResultDto(Math.Max(0, succeeded), failures);
    }
}
