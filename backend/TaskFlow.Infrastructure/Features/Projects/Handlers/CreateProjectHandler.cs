using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tenancy;
using TaskFlow.Infrastructure.Features.Dashboard;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class CreateProjectHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IMapper mapper,
    IActivityLogger activityLogger,
    IMemoryCache cache)
    : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (!currentTenant.IsSet)
        {
            throw new TenantContextMissingException();
        }

        var now = DateTime.UtcNow;

        var project = new Domain.Entities.Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentTenant.OrganizationId,
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        await dbContext.Projects.AddAsync(project, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        DashboardCacheInvalidation.InvalidateOrganizationStats(cache, currentTenant.OrganizationId);
        DashboardCacheInvalidation.InvalidateMyStatsForUsers(cache, currentUser.UserId);

        if (currentUser.UserId is { } actorId)
        {
            var actor = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
            var actorName = actor?.UserName ?? string.Empty;
            await activityLogger.LogAsync(
                ActivityEntityTypes.Project,
                project.Id,
                ActivityActions.ProjectCreated,
                actorId,
                actorName,
                currentTenant.OrganizationId,
                new { name = project.Name },
                cancellationToken);
        }

        return mapper.Map<ProjectDto>(project);
    }
}

