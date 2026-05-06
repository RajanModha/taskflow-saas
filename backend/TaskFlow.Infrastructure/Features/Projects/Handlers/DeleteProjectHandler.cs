using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Workspaces;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class DeleteProjectHandler(
    IProjectWriteRepository projectRepository,
    ICurrentUser currentUser,
    ICurrentUserService currentUserService,
    IMemoryCache cache,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    IWebhookDispatcher webhookDispatcher)
    : IRequestHandler<DeleteProjectCommand, bool>
{
    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var deleted = await projectRepository.SoftDeleteProjectAsync(request.ProjectId, cancellationToken);
        if (deleted is null || !deleted.Deleted)
        {
            return false;
        }

        if (currentUser.UserId is { } actorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Project,
                deleted.ProjectId,
                ActivityActions.ProjectDeleted,
                actorId,
                currentUserService.UserName,
                deleted.OrganizationId,
                new { name = deleted.Name },
                cancellationToken);
        }
        boardCacheVersion.RemoveProject(deleted.ProjectId);

        await webhookDispatcher.DispatchOrganizationEventAsync(
            deleted.OrganizationId,
            WebhookEventTypes.ProjectDeleted,
            new { projectId = deleted.ProjectId, name = deleted.Name },
            cancellationToken);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, deleted.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId);
        return true;
    }
}

