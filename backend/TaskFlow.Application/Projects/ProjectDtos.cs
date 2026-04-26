using MediatR;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Projects;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateProjectCommand(string Name, string? Description) : IRequest<ProjectDto>;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string? Description) : IRequest<ProjectDto?>;

public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest<bool>;
public sealed record RestoreProjectCommand(Guid ProjectId) : IRequest<ProjectDto?>;

public sealed record GetProjectsQuery(
    int Page,
    int PageSize,
    string? Q,
    string? SortBy,
    bool SortDesc) : IRequest<PagedResultDto<ProjectDto>>;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDto?>;

public sealed record ProjectExportPayload(
    ProjectDto Project,
    IReadOnlyList<TaskDto> Tasks,
    DateTime ExportedAtUtc,
    string ExportedBy,
    int TotalTasks);

public sealed record GetProjectExportQuery(Guid ProjectId) : IRequest<GetProjectExportQueryResponse>;

public sealed record GetProjectExportQueryResponse(
    bool ProjectNotFound,
    bool TaskLimitExceeded,
    ProjectExportPayload? Payload);

