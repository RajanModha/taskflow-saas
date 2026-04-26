using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tenancy;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class RestoreProjectHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    IMapper mapper,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion)
    : IRequestHandler<RestoreProjectCommand, ProjectDto?>
{
    public async Task<ProjectDto?> Handle(RestoreProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.Id == request.ProjectId &&
                     currentTenant.IsSet &&
                     p.OrganizationId == currentTenant.OrganizationId,
                cancellationToken);
        if (project is null || !project.IsDeleted)
        {
            return null;
        }

        project.IsDeleted = false;
        project.DeletedAt = null;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, project.OrganizationId);
        boardCacheVersion.BumpProject(project.Id);
        return mapper.Map<ProjectDto>(project);
    }
}
