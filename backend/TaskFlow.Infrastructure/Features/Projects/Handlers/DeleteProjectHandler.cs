using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class DeleteProjectHandler(
    TaskFlowDbContext dbContext,
    IMemoryCache cache)
    : IRequestHandler<DeleteProjectCommand, bool>
{
    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return false;
        }

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        cache.Remove(DashboardCacheKeys.DashboardStats(project.OrganizationId));
        return true;
    }
}

