using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class AddTaskDependencyCommandHandler(
    TaskFlowDbContext dbContext,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<AddTaskDependencyCommand, AddTaskDependencyResult>
{
    public async Task<AddTaskDependencyResult> Handle(AddTaskDependencyCommand request, CancellationToken cancellationToken)
    {
        if (request.TaskId == request.BlockingTaskId)
        {
            return new AddTaskDependencyResult.SelfReference();
        }

        var blocked = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        var blocking = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.BlockingTaskId, cancellationToken);

        if (blocked is null || blocking is null)
        {
            return new AddTaskDependencyResult.NotFound();
        }

        if (blocked.OrganizationId != blocking.OrganizationId)
        {
            return new AddTaskDependencyResult.NotFound();
        }

        var existing = await dbContext.TaskDependencies
            .AnyAsync(
                d => d.BlockedTaskId == request.TaskId && d.BlockingTaskId == request.BlockingTaskId,
                cancellationToken);
        if (existing)
        {
            return new AddTaskDependencyResult.Duplicate();
        }

        var count = await dbContext.TaskDependencies
            .CountAsync(d => d.BlockedTaskId == request.TaskId, cancellationToken);
        if (count >= 10)
        {
            return new AddTaskDependencyResult.MaxDependencies();
        }

        if (await WouldCreateCycleAsync(dbContext, request.TaskId, request.BlockingTaskId, cancellationToken))
        {
            return new AddTaskDependencyResult.Cycle();
        }

        dbContext.TaskDependencies.Add(
            new TaskDependency
            {
                BlockedTaskId = request.TaskId,
                BlockingTaskId = request.BlockingTaskId,
            });
        await dbContext.SaveChangesAsync(cancellationToken);

        boardCacheVersion.BumpProject(blocked.ProjectId);
        if (blocking.ProjectId != blocked.ProjectId)
        {
            boardCacheVersion.BumpProject(blocking.ProjectId);
        }

        var dto = new DependencyDto(
            request.TaskId,
            new TaskBlockingSummaryDto(blocking.Id, blocking.Title, blocking.Status));
        return new AddTaskDependencyResult.Ok(dto);
    }

    private static async Task<bool> WouldCreateCycleAsync(
        TaskFlowDbContext db,
        Guid from,
        Guid to,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(to);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == from)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            var deps = await db.TaskDependencies
                .AsNoTracking()
                .Where(d => d.BlockedTaskId == current)
                .Select(d => d.BlockingTaskId)
                .ToListAsync(cancellationToken);
            foreach (var d in deps)
            {
                queue.Enqueue(d);
            }
        }

        return false;
    }
}

public sealed class RemoveTaskDependencyCommandHandler(
    TaskFlowDbContext dbContext,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<RemoveTaskDependencyCommand, bool>
{
    public async Task<bool> Handle(RemoveTaskDependencyCommand request, CancellationToken cancellationToken)
    {
        var blocked = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        var blocking = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.BlockingTaskId, cancellationToken);
        if (blocked is null || blocking is null)
        {
            return false;
        }

        var deleted = await dbContext.TaskDependencies
            .Where(d => d.BlockedTaskId == request.TaskId && d.BlockingTaskId == request.BlockingTaskId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
        if (deleted)
        {
            boardCacheVersion.BumpProject(blocked.ProjectId);
            if (blocking.ProjectId != blocked.ProjectId)
            {
                boardCacheVersion.BumpProject(blocking.ProjectId);
            }
        }

        return deleted;
    }
}

public sealed class GetTaskDependenciesQueryHandler(TaskFlowDbContext dbContext)
    : IRequestHandler<GetTaskDependenciesQuery, TaskDependenciesResponse?>
{
    public async Task<TaskDependenciesResponse?> Handle(GetTaskDependenciesQuery request, CancellationToken cancellationToken)
    {
        var me = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        if (me is null)
        {
            return null;
        }

        var blockedByRows = await (
                from d in dbContext.TaskDependencies.AsNoTracking()
                join b in dbContext.Tasks.AsNoTracking() on d.BlockingTaskId equals b.Id
                where d.BlockedTaskId == request.TaskId
                select new { b.Id, b.Title, b.Status })
            .ToListAsync(cancellationToken);

        var blockedBy = blockedByRows
            .Select(r => new DependencyDto(
                request.TaskId,
                new TaskBlockingSummaryDto(r.Id, r.Title, r.Status)))
            .ToList();

        var blockedIds = await dbContext.TaskDependencies
            .AsNoTracking()
            .Where(d => d.BlockingTaskId == request.TaskId)
            .Select(d => d.BlockedTaskId)
            .ToListAsync(cancellationToken);

        var blockedTasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(t => blockedIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var blocking = blockedIds
            .Where(id => blockedTasks.ContainsKey(id))
            .Select(id =>
            {
                var b = blockedTasks[id];
                return new DependencyDto(
                    b.Id,
                    new TaskBlockingSummaryDto(me.Id, me.Title, me.Status));
            })
            .ToList();

        return new TaskDependenciesResponse(blockedBy, blocking);
    }
}
