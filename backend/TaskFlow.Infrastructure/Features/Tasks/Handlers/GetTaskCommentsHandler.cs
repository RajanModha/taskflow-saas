using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tenancy;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTaskCommentsHandler(TaskFlowDbContext dbContext, ICurrentTenant currentTenant)
    : IRequestHandler<GetTaskCommentsQuery, PagedResultDto<CommentDto>?>
{
    public async System.Threading.Tasks.Task<PagedResultDto<CommentDto>?> Handle(
        GetTaskCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var task = await TaskTenantGuard.GetTaskInCurrentTenantAsync(
            dbContext,
            currentTenant,
            request.TaskId,
            cancellationToken);

        if (task is null)
        {
            return null;
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var skip = (page - 1) * pageSize;

        var baseQuery = dbContext.Comments
            .AsNoTracking()
            .Where(c => c.TaskId == request.TaskId);

        var total = await baseQuery.LongCountAsync(cancellationToken);
        var rows = await baseQuery
            .OrderBy(c => c.CreatedAtUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var authorIds = rows.Where(c => !c.IsDeleted).Select(c => c.AuthorId).Distinct().ToList();
        var authors = await dbContext.Users
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var items = rows.Select(c =>
            {
                authors.TryGetValue(c.AuthorId, out var author);
                return CommentMapper.ToDto(c, author);
            })
            .ToList();

        return PagedResultDto<CommentDto>.Create(items, page, pageSize, total);
    }
}
