using MediatR;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Features.Projects.Handlers;

public sealed class GetProjectExportHandler(
    IProjectReadRepository projectReadRepository,
    ITaskReadModelAssembler taskReadModelAssembler,
    ICurrentUserService currentUser)
    : IRequestHandler<GetProjectExportQuery, GetProjectExportQueryResponse>
{
    public async Task<GetProjectExportQueryResponse> Handle(
        GetProjectExportQuery request,
        CancellationToken cancellationToken)
    {
        var exportData = await projectReadRepository.GetProjectExportDataAsync(request.ProjectId, cancellationToken);
        if (exportData is null)
        {
            return new GetProjectExportQueryResponse(true, false, null);
        }

        var count = exportData.Value.Tasks.Count;
        if (count > 10_000)
        {
            return new GetProjectExportQueryResponse(false, true, null);
        }

        var taskDtos = await taskReadModelAssembler.ToTaskDtosAsync(exportData.Value.Tasks, cancellationToken);
        var project = exportData.Value.Project;
        var projectDto = new ProjectDto(project.Id, project.Name, project.Description, project.CreatedAtUtc, project.UpdatedAtUtc);
        var exportedBy = string.IsNullOrWhiteSpace(currentUser.UserName)
            ? currentUser.UserId.ToString()
            : currentUser.UserName;

        var payload = new ProjectExportPayload(
            projectDto,
            taskDtos,
            DateTime.UtcNow,
            exportedBy,
            (int)count);

        return new GetProjectExportQueryResponse(false, false, payload);
    }
}
