using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using DomainTaskPriority = TaskFlow.Domain.Entities.TaskPriority;
using MediatR;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.Tasks;

public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateTaskCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc) : IRequest<TaskDto?>;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc) : IRequest<TaskDto?>;

public sealed record DeleteTaskCommand(Guid TaskId) : IRequest<bool>;

public sealed record GetTasksQuery(
    int Page,
    int PageSize,
    Guid? ProjectId,
    DomainTaskStatus? Status,
    DomainTaskPriority? Priority,
    DateTime? DueFromUtc,
    DateTime? DueToUtc,
    string? Q,
    string? SortBy,
    bool SortDesc) : IRequest<PagedResultDto<TaskDto>>;

public sealed record GetTaskByIdQuery(Guid TaskId) : IRequest<TaskDto?>;

