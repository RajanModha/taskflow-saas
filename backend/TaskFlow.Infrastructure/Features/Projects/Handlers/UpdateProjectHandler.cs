using MediatR;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Repositories;
using TaskFlow.Infrastructure.Features.Dashboard;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class UpdateProjectHandler(
    IProjectWriteRepository projectRepository,
    IProjectReadRepository projectReadRepository,
    ICurrentUser currentUser,
    ICurrentUserService currentUserService,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger,
    IMemoryCache cache)
    : IRequestHandler<UpdateProjectCommand, ProjectDto?>
{
    public async Task<ProjectDto?> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var result = await projectRepository.UpdateProjectAsync(
            request.ProjectId,
            request.Name,
            request.Description,
            cancellationToken);
        if (result is null)
        {
            return null;
        }

        if (currentUser.UserId is { } actorId)
        {
            await activityLogger.LogAsync(
                ActivityEntityTypes.Project,
                result.ProjectId,
                ActivityActions.ProjectUpdated,
                actorId,
                currentUserService.UserName,
                result.OrganizationId,
                new { previousName = result.PreviousName, previousDescription = result.PreviousDescription, name = result.Name, description = result.Description },
                cancellationToken);
        }

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, result.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId);

        boardCacheVersion.BumpProject(result.ProjectId);
        var project = await projectReadRepository.GetProjectByIdAsync(result.ProjectId, cancellationToken);
        return project is null
            ? null
            : new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAtUtc, project.UpdatedAtUtc);
    }
}

