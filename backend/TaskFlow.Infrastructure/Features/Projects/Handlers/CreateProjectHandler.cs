using AutoMapper;
using MediatR;
using TaskFlow.Application.Common;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Application.Tenancy;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class CreateProjectHandler(
    TaskFlowDbContext dbContext,
    ICurrentTenant currentTenant,
    IMapper mapper)
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

        return mapper.Map<ProjectDto>(project);
    }
}

