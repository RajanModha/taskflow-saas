using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class UpdateProjectHandler(
    TaskFlowDbContext dbContext,
    ICurrentUser currentUser,
    IMapper mapper,
    IBoardCacheVersion boardCacheVersion,
    IActivityLogger activityLogger)
    : IRequestHandler<UpdateProjectCommand, ProjectDto?>
{
    public async Task<ProjectDto?> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return null;
        }

        var previousName = project.Name;
        var previousDescription = project.Description;

        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        if (currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Project,
                project.Id,
                ActivityActions.ProjectUpdated,
                actorId,
                actorName,
                project.OrganizationId,
                new { previousName, previousDescription, name = project.Name, description = project.Description },
                cancellationToken);
        }

        boardCacheVersion.BumpProject(project.Id);
        return mapper.Map<ProjectDto>(project);
    }
}

