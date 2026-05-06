using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Workspaces;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class CreateProjectHandler(
    IProjectWriteRepository projectRepository,
    ICurrentUser currentUser,
    ICurrentUserService currentUserService,
    IActivityLogger activityLogger,
    IMemoryCache cache,
    IWebhookDispatcher webhookDispatcher)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var created = await projectRepository.CreateProjectAsync(request.Name, request.Description, cancellationToken);
        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, created.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId);

        if (currentUser.UserId is { } actorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Project,
                created.ProjectId,
                ActivityActions.ProjectCreated,
                actorId,
                currentUserService.UserName,
                created.OrganizationId,
                new { name = created.Name },
                cancellationToken);
        }

        await webhookDispatcher.DispatchOrganizationEventAsync(
            created.OrganizationId,
            WebhookEventTypes.ProjectCreated,
            new { projectId = created.ProjectId, name = created.Name },
            cancellationToken);

        return new ProjectDto(
            created.ProjectId,
            created.Name,
            created.Description,
            created.CreatedAtUtc,
            created.UpdatedAtUtc);
    }
}

