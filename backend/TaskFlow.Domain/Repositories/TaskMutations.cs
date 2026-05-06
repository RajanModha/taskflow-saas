using DomainTaskPriority = TaskFlow.Domain.Entities.TaskPriority;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Domain.Repositories;

public sealed record PatchTaskMutationInput(
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
    bool HasAssigneeId);

public sealed record AssignTaskMutationResult(
    Guid TaskId,
    string TaskTitle,
    Guid OrganizationId,
    Guid ProjectId,
    string ProjectName,
    Guid? PreviousAssigneeId,
    Guid? CurrentAssigneeId,
    string? CurrentAssigneeEmail,
    string? CurrentAssigneeUserName,
    string? CurrentAssigneeDisplayName,
    string? PreviousAssigneeUserName);

public sealed record PatchTaskMutationResult(
    Guid TaskId,
    Guid OrganizationId,
    Guid ProjectId,
    Guid? PreviousAssigneeId,
    Guid? CurrentAssigneeId,
    DateTime? PreviousDueDateUtc,
    DateTime? CurrentDueDateUtc,
    DomainTaskStatus PreviousStatus,
    DomainTaskStatus CurrentStatus);

public sealed record UpdateTaskMutationInput(
    Guid TaskId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId,
    IReadOnlyList<Guid>? TagIds,
    Guid? MilestoneId);

public sealed record UpdateTaskMutationResult(
    Guid TaskId,
    string TaskTitle,
    Guid OrganizationId,
    Guid ProjectId,
    string ProjectName,
    Guid? PreviousAssigneeId,
    Guid? CurrentAssigneeId,
    string? PreviousAssigneeUserName,
    string? CurrentAssigneeDisplayName,
    string? CurrentAssigneeUserName,
    string? CurrentAssigneeEmail,
    DomainTaskStatus PreviousStatus,
    DomainTaskStatus CurrentStatus,
    DomainTaskPriority PreviousPriority,
    DomainTaskPriority CurrentPriority,
    DateTime? PreviousDueDateUtc,
    DateTime? CurrentDueDateUtc);

public sealed record DeleteTaskMutationResult(
    Guid TaskId,
    Guid OrganizationId,
    Guid ProjectId,
    Guid? AssigneeId,
    string Title);

public sealed record BulkTaskMutationItem(
    Guid TaskId,
    Guid OrganizationId,
    Guid ProjectId,
    Guid? PreviousAssigneeId,
    Guid? CurrentAssigneeId);

public sealed record BulkTaskDeleteMutationResult(
    IReadOnlyList<BulkTaskMutationItem> Mutated,
    IReadOnlyList<Guid> NotFound);

public sealed record BulkTaskUpdateMutationInput(
    IReadOnlyList<Guid> TaskIds,
    DomainTaskStatus? Status,
    DomainTaskPriority? Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId,
    bool HasDueDateUtc,
    bool HasAssigneeId);

public sealed record BulkTaskUpdateMutationResult(
    IReadOnlyList<BulkTaskMutationItem> Mutated,
    IReadOnlyList<Guid> NotFound,
    bool InvalidAssignee,
    string? AssigneeEmail = null,
    string? AssigneeUserName = null,
    string? AssigneeDisplayName = null,
    string? WorkspaceName = null);

public sealed record ChecklistMutationResult(
    Guid TaskId,
    Guid OrganizationId,
    Guid ProjectId,
    Guid? AssigneeId,
    TaskChecklistItemReadModel Item,
    bool WasCompleted,
    bool IsNowCompleted,
    bool HasIncompleteItems);

public sealed record ChecklistDeleteMutationResult(
    bool Deleted,
    Guid TaskId,
    Guid OrganizationId,
    Guid ProjectId,
    Guid? AssigneeId);

public sealed record TaskCommentMutationResult(
    int StatusCode,
    Guid TaskId,
    Guid OrganizationId,
    Guid ProjectId,
    Guid? AssigneeId,
    TaskCommentReadModel? Comment);

public sealed record TaskTagMutationResult(
    bool TaskFound,
    bool TagFound,
    bool Changed,
    Guid TaskId,
    Guid OrganizationId,
    Guid ProjectId,
    Guid? AssigneeId,
    Guid TagId);

public sealed record TaskDependencyAddMutationResult(
    string Outcome,
    Guid BlockedTaskId,
    Guid? BlockingTaskId,
    string? BlockingTaskTitle,
    DomainTaskStatus? BlockingTaskStatus,
    Guid? BlockedProjectId,
    Guid? BlockingProjectId);

public sealed record TaskDependencyRemoveMutationResult(
    bool Deleted,
    Guid? BlockedProjectId,
    Guid? BlockingProjectId);

public sealed record CreateTaskMutationInput(
    Guid ProjectId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DomainTaskPriority Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId,
    IReadOnlyList<Guid>? TagIds,
    Guid? MilestoneId);

public sealed record CreateTaskMutationResult(
    Guid TaskId,
    string TaskTitle,
    Guid OrganizationId,
    Guid ProjectId,
    string ProjectName,
    Guid? AssigneeId,
    string? AssigneeEmail,
    string? AssigneeUserName,
    string? AssigneeDisplayName);

public sealed record CreateTaskFromTemplateMutationInput(
    Guid TemplateId,
    Guid ProjectId,
    string? OverrideTitle,
    string? OverrideDescription,
    DomainTaskPriority? OverridePriority,
    DateTime? OverrideDueDateUtc,
    Guid? OverrideAssigneeId);

