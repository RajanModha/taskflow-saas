using DomainTaskPriority = TaskFlow.Domain.Entities.TaskPriority;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using MediatR;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Projects;

public sealed record BoardColumnDto(
    string Status,
    int StatusValue,
    string DisplayName,
    string Color,
    int TaskCount,
    IReadOnlyList<BoardTaskDto> Tasks);

public sealed record BoardTaskDto(
    Guid Id,
    string Title,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc,
    bool IsOverdue,
    TaskAssigneeDto? Assignee,
    IReadOnlyList<TagDto> Tags,
    int CommentCount,
    DateTime CreatedAt);

public sealed record ProjectBoardResponse(
    Guid ProjectId,
    string ProjectName,
    IReadOnlyList<BoardColumnDto> Columns);

public sealed record GetProjectBoardQuery(
    Guid ProjectId,
    Guid? AssigneeId,
    Guid? TagId,
    string? Q) : IRequest<ProjectBoardResponse?>;

public sealed record MoveProjectBoardTaskCommand(
    Guid ProjectId,
    Guid TaskId,
    DomainTaskStatus NewStatus) : IRequest<BoardTaskDto?>;
