using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetOverdueTasksHandler(TaskFlowDbContext dbContext)
    : IRequestHandler<GetOverdueTasksQuery, PagedResultDto<TaskDto>>
{
    public async System.Threading.Tasks.Task<PagedResultDto<TaskDto>> Handle(
        GetOverdueTasksQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var skip = (page - 1) * pageSize;
        var now = DateTime.UtcNow;

        var query = dbContext.Tasks
            .AsNoTracking()
            .Where(t =>
                t.DueDateUtc.HasValue &&
                t.DueDateUtc < now &&
                t.Status != TaskFlow.Domain.Entities.TaskStatus.Done &&
                t.Status != TaskFlow.Domain.Entities.TaskStatus.Cancelled)
            .OrderBy(t => t.DueDateUtc);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
        var mapped = await TaskProjection.ToDtosAsync(dbContext, items, cancellationToken);
        return new PagedResultDto<TaskDto>(mapped, page, pageSize, total);
    }
}
