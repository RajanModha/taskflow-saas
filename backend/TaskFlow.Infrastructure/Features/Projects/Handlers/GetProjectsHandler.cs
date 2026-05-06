using MediatR;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class GetProjectsHandler(
    IProjectReadRepository projectRepository)
    : IRequestHandler<GetProjectsQuery, PagedResultDto<ProjectDto>>
{
    public async Task<PagedResultDto<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var paged = await projectRepository.GetPagedProjectsAsync(
            new ProjectListCriteria(request.Page, request.PageSize, request.Q, request.SortBy, request.SortDesc),
            cancellationToken);
        var items = paged.Items
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToList();
        return PagedResultDto<ProjectDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }
}

