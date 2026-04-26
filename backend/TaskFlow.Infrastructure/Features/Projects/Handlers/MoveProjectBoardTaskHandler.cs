using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class MoveProjectBoardTaskHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
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
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(
                t => t.Id == request.TaskId && t.ProjectId == request.ProjectId,
                cancellationToken);

        if (task is null)
        {
            return null;
        }

        var previous = task.Status;
        if (previous == request.NewStatus)
        {
            return await LoadBoardTaskDtoAsync(task.Id, cancellationToken);
        }

        task.Status = request.NewStatus;
        task.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Task,
                task.Id,
                ActivityActions.TaskStatusChanged,
                actorId,
                actorName,
                task.OrganizationId,
                new { from = previous.ToString(), to = request.NewStatus.ToString(), projectId = task.ProjectId },
                cancellationToken);
        }

        boardCacheVersion.BumpProject(task.ProjectId);
        DashboardCacheInvalidation.InvalidateAfterTaskMutation(
            cache,
            task.OrganizationId,
            currentUser.UserId,
            task.AssigneeId,
            task.AssigneeId);

        return await LoadBoardTaskDtoAsync(task.Id, cancellationToken);
    }

    private async System.Threading.Tasks.Task<BoardTaskDto?> LoadBoardTaskDtoAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var commentCount = await dbContext.Comments
            .AsNoTracking()
            .CountAsync(c => c.TaskId == taskId && !c.IsDeleted, cancellationToken);

        Dictionary<Guid, ApplicationUser> assignees = new();
        if (task.AssigneeId is { } aid)
        {
            var u = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == aid, cancellationToken);
            if (u is not null)
            {
                assignees = new Dictionary<Guid, ApplicationUser> { [u.Id] = u };
            }
        }

        var counts = new Dictionary<Guid, int> { [task.Id] = commentCount };
        return BoardMapper.ToBoardTask(task, assignees, counts, nowUtc);
    }
}
