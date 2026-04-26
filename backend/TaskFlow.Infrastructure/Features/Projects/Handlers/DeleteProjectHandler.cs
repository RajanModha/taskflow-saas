using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class DeleteProjectHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger)
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

        var orgId = project.OrganizationId;
        var projectId = project.Id;
        var name = project.Name;

        if (currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Project,
                projectId,
                ActivityActions.ProjectDeleted,
                actorId,
                actorName,
                orgId,
                new { name },
                cancellationToken);
        }

        boardCacheVersion.RemoveProject(project.Id);
        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        cache.Remove(DashboardCacheKeys.DashboardStats(orgId));
        return true;
    }
}

