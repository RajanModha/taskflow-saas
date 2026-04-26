using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTasksHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetTasksQuery, PagedResultDto<TaskDto>>
{
    public async System.Threading.Tasks.Task<PagedResultDto<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var skip = (page - 1) * pageSize;

        var query = dbContext.Tasks.AsNoTracking().AsQueryable();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == request.ProjectId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == request.Priority.Value);
        }

        if (request.DueFromUtc.HasValue)
        {
            query = query.Where(t => t.DueDateUtc.HasValue && t.DueDateUtc.Value >= request.DueFromUtc.Value);
        }

        if (request.DueToUtc.HasValue)
        {
            query = query.Where(t => t.DueDateUtc.HasValue && t.DueDateUtc.Value <= request.DueToUtc.Value);
        }

        if (request.AssignedToMe == true)
        {
            if (currentUser.UserId is { } me)
            {
                query = query.Where(t => t.AssigneeId == me);
            }
            else
            {
                query = query.Where(_ => false);
            }
        }

        if (request.AssigneeId.HasValue)
        {
            query = query.Where(t => t.AssigneeId == request.AssigneeId.Value);
        }

        if (request.TagId.HasValue)
        {
            var tagId = request.TagId.Value;
            query = query.Where(t => dbContext.TaskTags.Any(tt => tt.TaskId == t.Id && tt.TagId == tagId));
        }

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var q = request.Q.Trim();
            query = query.Where(t => t.Title.Contains(q));
        }

        var sortBy = request.SortBy?.Trim().ToLowerInvariant();

        query = sortBy switch
        {
            "duedateutc" => request.SortDesc
                ? query.OrderByDescending(t => t.DueDateUtc ?? DateTime.MaxValue)
                : query.OrderBy(t => t.DueDateUtc ?? DateTime.MaxValue),
            "priority" => request.SortDesc
                ? query.OrderByDescending(t => (int)t.Priority)
                : query.OrderBy(t => (int)t.Priority),
            "status" => request.SortDesc
                ? query.OrderByDescending(t => (int)t.Status)
                : query.OrderBy(t => (int)t.Status),
            "createdatutc" or null or "" => request.SortDesc
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc),
            _ => request.SortDesc
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc),
        };

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        var mapped = await TaskProjection.ToDtosAsync(dbContext, items, cancellationToken);
        return new PagedResultDto<TaskDto>(mapped, page, pageSize, total);
    }
}
