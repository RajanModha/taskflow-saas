using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Infrastructure.Activity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskActivityHandler(TaskFlowDbContext dbContext)
    : IRequestHandler<GetTaskActivityQuery, PagedResultDto<ActivityLogDto>?>
{
    public async Task<PagedResultDto<ActivityLogDto>?> Handle(
        GetTaskActivityQuery request,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var skip = (page - 1) * pageSize;

        var query = dbContext.ActivityLogs
            .AsNoTracking()
            .Where(
                a => a.EntityType == ActivityEntityTypes.Task &&
                     a.EntityId == request.TaskId);

        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(ActivityLogMapper.ToDto).ToList();
        return new PagedResultDto<ActivityLogDto>(items, page, pageSize, total);
    }
}
