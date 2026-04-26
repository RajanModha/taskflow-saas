using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class AddTaskTagHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger)
    : IRequestHandler<AddTaskTagCommand, TaskDto?>
{
    public async System.Threading.Tasks.Task<TaskDto?> Handle(AddTaskTagCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        var tagExists = await dbContext.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TagId && t.OrganizationId == task.OrganizationId, cancellationToken);
        if (!tagExists)
        {
            return null;
        }

        var already = await dbContext.TaskTags
            .AsNoTracking()
            .AnyAsync(tt => tt.TaskId == request.TaskId && tt.TagId == request.TagId, cancellationToken);
        if (already)
        {
            var unchanged = await dbContext.Tasks.AsNoTracking()
                .FirstAsync(t => t.Id == request.TaskId, cancellationToken);
            var dtoSame = await TaskProjection.ToDtosAsync(dbContext, [unchanged], cancellationToken);
            return dtoSame[0];
        }

        await dbContext.TaskTags.AddAsync(
            new TaskTag
            {
                TaskId = request.TaskId,
                TagId = request.TagId,
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (currentUser.UserId is { } actorId)
        {
            var tag = await dbContext.Tags.AsNoTracking().FirstAsync(t => t.Id == request.TagId, cancellationToken);
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                task.Id,
                ActivityActions.TaskTagAdded,
                actorId,
                actorName,
                task.OrganizationId,
                new { tagId = tag.Id, tagName = tag.Name },
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, task.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, task.AssigneeId);
        boardCacheVersion.BumpProject(task.ProjectId);

        var refreshed = await dbContext.Tasks.AsNoTracking()
            .FirstAsync(t => t.Id == request.TaskId, cancellationToken);
        var dtoList = await TaskProjection.ToDtosAsync(dbContext, [refreshed], cancellationToken);
        return dtoList[0];
    }
}
