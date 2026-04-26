using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class RemoveTaskTagHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<RemoveTaskTagCommand, int>
{
    public async System.Threading.Tasks.Task<int> Handle(RemoveTaskTagCommand request, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return StatusCodes.Status404NotFound;
        }

        var projectId = task.ProjectId;
        var removed = await dbContext.TaskTags
            .Where(tt => tt.TaskId == request.TaskId && tt.TagId == request.TagId)
            .ExecuteDeleteAsync(cancellationToken);

        if (removed > 0)
        {
            DashboardCacheInvalidation.InvalidateOrganizationStats(cache, task.OrganizationId);
            DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId, task.AssigneeId);
            boardCacheVersion.BumpProject(projectId);
        }

        return StatusCodes.Status204NoContent;
    }
}
