using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class GetProjectsHandler(
    TaskFlowDbContext dbContext,
    IMapper mapper)
    : IRequestHandler<GetProjectsQuery, PagedResultDto<ProjectDto>>
{
    public async Task<PagedResultDto<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var skip = (page - 1) * pageSize;

        var query = dbContext.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var q = request.Q.Trim();
            query = query.Where(p => p.Name.Contains(q));
        }

        query = request.SortBy?.Trim().ToLowerInvariant() switch
        {
            null or "" or "createdatutc" => request.SortDesc ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
            "name" => request.SortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => request.SortDesc ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
        };

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
        var mapped = mapper.Map<List<ProjectDto>>(items);

        return PagedResultDto<ProjectDto>.Create(mapped, page, pageSize, total);
    }
}

