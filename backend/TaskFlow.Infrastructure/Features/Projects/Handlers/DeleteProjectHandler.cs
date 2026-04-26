using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Workspaces;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class DeleteProjectHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    IWebhookDispatcher webhookDispatcher)
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
        var now = DateTime.UtcNow;

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

        project.IsDeleted = true;
        project.DeletedAt = now;
        project.UpdatedAtUtc = now;

        var projectTasks = await dbContext.Tasks
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var task in projectTasks)
        {
            task.IsDeleted = true;
            task.DeletedAt = now;
            task.UpdatedAtUtc = now;
        }

        boardCacheVersion.RemoveProject(project.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        await webhookDispatcher.DispatchOrganizationEventAsync(
            orgId,
            WebhookEventTypes.ProjectDeleted,
            new { projectId, name },
            cancellationToken);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, orgId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId);
        return true;
    }
}

