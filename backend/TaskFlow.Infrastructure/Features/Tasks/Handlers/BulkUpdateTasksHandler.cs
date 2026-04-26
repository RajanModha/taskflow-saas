using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class BulkUpdateTasksHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger)
    : IRequestHandler<BulkUpdateTasksCommand, BulkTaskOperationResultDto>
{
    public async Task<BulkTaskOperationResultDto> Handle(BulkUpdateTasksCommand request, CancellationToken cancellationToken)
    {
        var ids = request.TaskIds.Distinct().ToArray();
        var tasks = await dbContext.Tasks
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var failures = new List<BulkTaskFailureDto>();
        var foundIds = tasks.Select(t => t.Id).ToHashSet();
        failures.AddRange(ids.Where(id => !foundIds.Contains(id)).Select(id => new BulkTaskFailureDto(id, "not_found")));

        if (request.Updates.AssigneeId is { } assigneeId)
        {
            var assigneeExists = await dbContext.Users.AnyAsync(
                u => u.Id == assigneeId && tasks.Select(t => t.OrganizationId).Contains(u.OrganizationId),
                cancellationToken);
            if (!assigneeExists)
            {
                return new BulkTaskOperationResultDto(0, [new BulkTaskFailureDto(Guid.Empty, "invalid_assignee")]);
            }
        }

        var now = DateTime.UtcNow;
        foreach (var task in tasks)
        {
            if (request.Updates.Status is { } status)
            {
                task.Status = status;
            }

            if (request.Updates.Priority is { } priority)
            {
                task.Priority = priority;
            }

            if (request.Updates.HasDueDateUtc)
            {
                task.DueDateUtc = request.Updates.DueDateUtc;
            }

            if (request.Updates.HasAssigneeId)
            {
                task.AssigneeId = request.Updates.AssigneeId;
            }

            task.UpdatedAtUtc = now;
            task.ReminderSent = false;
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var conflictedIds = ex.Entries
                .Where(e => e.Entity is DomainTask)
                .Select(e => ((DomainTask)e.Entity).Id)
                .Distinct()
                .ToHashSet();
            failures.AddRange(conflictedIds.Select(id => new BulkTaskFailureDto(id, "concurrency_conflict")));

            foreach (var entry in ex.Entries)
            {
                if (entry.Entity is DomainTask task && conflictedIds.Contains(task.Id))
                {
                    entry.State = EntityState.Detached;
                }
            }

            await tx.RollbackAsync(cancellationToken);
            await using var retryTx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await retryTx.CommitAsync(cancellationToken);
        }

        if (currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            foreach (var task in tasks.Where(t => failures.All(f => f.TaskId != t.Id)))
            {
                await activityLogger.LogAsync(
                    ActivityEntityTypes.Task,
                    task.Id,
                    ActivityActions.TaskBulkUpdated,
                    actorId,
                    actorName,
                    task.OrganizationId,
                    new { bulk = true },
                    cancellationToken);
            }
        }

        foreach (var task in tasks.Where(t => failures.All(f => f.TaskId != t.Id)))
        {
            DashboardCacheInvalidation.InvalidateAfterTaskMutation(
                cache,
                task.OrganizationId,
                currentUser.UserId,
                null,
                task.AssigneeId);
            boardCacheVersion.BumpProject(task.ProjectId);
        }

        var succeeded = tasks.Count - failures.Count(f => f.TaskId != Guid.Empty);
        return new BulkTaskOperationResultDto(Math.Max(0, succeeded), failures);
    }
}
