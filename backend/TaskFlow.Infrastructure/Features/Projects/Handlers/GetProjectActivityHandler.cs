using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tenancy;
using TaskFlow.Infrastructure.Activity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class GetProjectActivityHandler(TaskFlowDbContext dbContext)
    : IRequestHandler<GetProjectActivityQuery, PagedResultDto<ActivityLogDto>?>
{
    public async Task<PagedResultDto<ActivityLogDto>?> Handle(
        GetProjectActivityQuery request,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return null;
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var skip = (page - 1) * pageSize;
        var projectId = request.ProjectId;

        var query = dbContext.ActivityLogs
            .AsNoTracking()
            .Where(
                a =>
                    (a.EntityType == ActivityEntityTypes.Project && a.EntityId == projectId) ||
                    (a.EntityType == ActivityEntityTypes.Task &&
                     dbContext.Tasks.Any(t => t.Id == a.EntityId && t.ProjectId == projectId)));

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
