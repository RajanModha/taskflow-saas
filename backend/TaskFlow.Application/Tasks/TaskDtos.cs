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
    DateTime UpdatedAtUtc,
    TaskAssigneeDto? Assignee,
    int CommentCount,
    IReadOnlyList<TagDto> Tags,
    int ChecklistTotal,
    int ChecklistCompleted,
    decimal ChecklistProgress,
    bool IsDeleted,
    DateTime? DeletedAt);

public sealed record ChecklistItemDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    int Order,
    DateTime? CompletedAt);

public sealed record GetTaskChecklistQuery(Guid TaskId) : IRequest<IReadOnlyList<ChecklistItemDto>?>;

public sealed record AddChecklistItemCommand(Guid TaskId, string Title, int? InsertAfterOrder) : IRequest<ChecklistItemDto?>;

public sealed record UpdateChecklistItemCommand(Guid TaskId, Guid ItemId, string? Title, bool? IsCompleted) : IRequest<ChecklistItemDto?>;

public sealed record DeleteChecklistItemCommand(Guid TaskId, Guid ItemId) : IRequest<bool>;

public sealed record ReorderChecklistCommand(Guid TaskId, Guid[] OrderedIds) : IRequest<IReadOnlyList<ChecklistItemDto>?>;

public sealed record CreateTaskCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId = null,
    Guid[]? TagIds = null) : IRequest<TaskDto?>;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId,
    Guid[]? TagIds) : IRequest<TaskDto?>;

public sealed record DeleteTaskCommand(Guid TaskId) : IRequest<bool>;
public sealed record RestoreTaskCommand(Guid TaskId) : IRequest<TaskDto?>;
public sealed record PermanentDeleteTaskCommand(Guid TaskId) : IRequest<bool>;

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
    bool SortDesc,
    bool? AssignedToMe,
    Guid? AssigneeId,
    Guid? TagId,
    bool IncludeDeleted) : IRequest<PagedResultDto<TaskDto>>;

public sealed record AssignTaskCommand(Guid TaskId, Guid? AssigneeId) : IRequest<TaskDto?>;

public sealed record GetOverdueTasksQuery(int Page, int PageSize) : IRequest<PagedResultDto<TaskDto>>;

public sealed record GetTaskByIdQuery(Guid TaskId) : IRequest<TaskDto?>;

public sealed record AddTaskTagCommand(Guid TaskId, Guid TagId) : IRequest<TaskDto?>;

public sealed record RemoveTaskTagCommand(Guid TaskId, Guid TagId) : IRequest<int>;

