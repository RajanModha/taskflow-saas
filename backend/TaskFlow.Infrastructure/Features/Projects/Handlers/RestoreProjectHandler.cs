using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class RestoreProjectHandler(
    IProjectWriteRepository projectWriteRepository,
    IProjectReadRepository projectReadRepository,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<RestoreProjectCommand, ProjectDto?>
{
    public async Task<ProjectDto?> Handle(RestoreProjectCommand request, CancellationToken cancellationToken)
    {
        var restored = await projectWriteRepository.RestoreProjectAsync(request.ProjectId, cancellationToken);
        if (restored is null)
        {
            return null;
        }
        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, restored.OrganizationId);
        boardCacheVersion.BumpProject(restored.ProjectId);
        var project = await projectReadRepository.GetProjectByIdAsync(restored.ProjectId, cancellationToken);
        return project is null
            ? null
            : new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAtUtc, project.UpdatedAtUtc);
    }
}
