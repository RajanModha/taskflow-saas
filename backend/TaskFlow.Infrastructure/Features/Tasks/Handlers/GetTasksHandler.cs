using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Infrastructure.Persistence;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Features.Tasks.Handlers;

public sealed class GetTasksHandler(
    TaskFlowDbContext dbContext,
    IMapper mapper) : IRequestHandler<GetTasksQuery, PagedResultDto<TaskDto>>
{
    public async Task<PagedResultDto<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
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

        // Ensure we map the correct generic type (DomainTask is an alias for the entity).
        var mapped = mapper.Map<List<TaskDto>>(items);
        return new PagedResultDto<TaskDto>(mapped, page, pageSize, total);
    }
}

