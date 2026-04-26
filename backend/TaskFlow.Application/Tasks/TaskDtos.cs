using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;
using DomainTaskPriority = TaskFlow.Domain.Entities.TaskPriority;
using MediatR;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.Tasks;

public sealed record TaskMilestoneDto(Guid Id, string Name);

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
    TaskMilestoneDto? Milestone,
    bool IsBlocked,
    int BlockerCount,
    int CommentCount,
    IReadOnlyList<TagDto> Tags,
    int ChecklistTotal,
    int ChecklistCompleted,
    decimal ChecklistProgress,
    bool IsDeleted,
    DateTime? DeletedAt,
    uint RowVersion);

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
    Guid[]? TagIds = null,
    Guid? MilestoneId = null) : IRequest<TaskDto?>;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId,
    Guid[]? TagIds,
    Guid? MilestoneId) : IRequest<TaskDto?>;

public sealed record DeleteTaskCommand(Guid TaskId) : IRequest<bool>;
public sealed record RestoreTaskCommand(Guid TaskId) : IRequest<TaskDto?>;
public sealed record PermanentDeleteTaskCommand(Guid TaskId) : IRequest<bool>;
public sealed record PatchTaskCommand(
    Guid TaskId,
    string? Title,
    bool HasTitle,
    string? Description,
    bool HasDescription,
    DomainTaskStatus? Status,
    bool HasStatus,
    DomainTaskPriority? Priority,
    bool HasPriority,
    DateTime? DueDateUtc,
    bool HasDueDateUtc,
    Guid? AssigneeId,
    bool HasAssigneeId) : IRequest<TaskDto?>;

public sealed record BulkTaskUpdateFields(
    DomainTaskStatus? Status,
    DomainTaskPriority? Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId,
    bool HasDueDateUtc,
    bool HasAssigneeId);

public sealed record BulkTaskFailureDto(Guid TaskId, string Reason);
public sealed record BulkTaskOperationResultDto(int Succeeded, IReadOnlyList<BulkTaskFailureDto> Failed);
public sealed record BulkTaskDeleteResultDto(int Deleted, IReadOnlyList<Guid> NotFound);

public sealed record BulkUpdateTasksCommand(Guid[] TaskIds, BulkTaskUpdateFields Updates)
    : IRequest<BulkTaskOperationResultDto>;
public sealed record BulkDeleteTasksCommand(Guid[] TaskIds) : IRequest<BulkTaskDeleteResultDto>;
public sealed record BulkAssignTasksCommand(Guid[] TaskIds, Guid? AssigneeId) : IRequest<BulkTaskOperationResultDto>;

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
    Guid? MilestoneId,
    bool? IsBlocked,
    bool IncludeDeleted) : IRequest<PagedResultDto<TaskDto>>;

public sealed record AssignTaskCommand(Guid TaskId, Guid? AssigneeId) : IRequest<TaskDto?>;

public sealed record GetOverdueTasksQuery(int Page, int PageSize) : IRequest<PagedResultDto<TaskDto>>;

public sealed record GetTaskByIdQuery(Guid TaskId) : IRequest<TaskDto?>;

public sealed record AddTaskTagCommand(Guid TaskId, Guid TagId) : IRequest<TaskDto?>;

public sealed record RemoveTaskTagCommand(Guid TaskId, Guid TagId) : IRequest<int>;

