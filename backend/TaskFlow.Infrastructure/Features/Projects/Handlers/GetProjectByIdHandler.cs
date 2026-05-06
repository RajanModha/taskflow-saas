using MediatR;
using TaskFlow.Application.Projects;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class GetProjectByIdHandler(
    IProjectReadRepository projectRepository)
    : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
{
    public async Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetProjectByIdAsync(request.ProjectId, cancellationToken);
        return project is null
            ? null
            : new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAtUtc, project.UpdatedAtUtc);
    }
}

