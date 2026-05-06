namespace TaskFlow.Domain.Repositories;

public sealed record ProjectReadModel(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ProjectListCriteria(
    int Page,
    int PageSize,
    string? Q,
    string? SortBy,
    bool SortDesc);

public sealed record CreateProjectMutationResult(
    Guid ProjectId,
    string Name,
    Guid OrganizationId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? Description);

public sealed record UpdateProjectMutationResult(
    Guid ProjectId,
    Guid OrganizationId,
    string PreviousName,
    string? PreviousDescription,
    string Name,
    string? Description);

public sealed record DeleteProjectMutationResult(
    Guid ProjectId,
    Guid OrganizationId,
    string Name,
    bool Deleted);

public sealed record RestoreProjectMutationResult(
    Guid ProjectId,
    Guid OrganizationId,
    string Name);

public sealed record ProjectActivityRow(
    Guid Id,
    string Action,
    Guid ActorId,
    string ActorName,
    DateTime OccurredAtUtc,
    string? Metadata);

public sealed record MoveProjectBoardTaskMutationResult(
    bool TaskFound,
    bool StatusChanged,
    Guid TaskId,
    Guid OrganizationId,
    Guid ProjectId,
    TaskFlow.Domain.Entities.TaskStatus PreviousStatus,
    Guid? AssigneeId);

public sealed record ProjectBoardTaskReadModel(
    Guid TaskId,
    string Title,
    TaskFlow.Domain.Entities.TaskStatus Status,
    TaskFlow.Domain.Entities.TaskPriority Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId,
    string? AssigneeUserName,
    string? AssigneeDisplayName,
    IReadOnlyList<(Guid Id, string Name, string Color)> Tags,
    int CommentCount,
    DateTime CreatedAtUtc);

public sealed record MilestoneReadModel(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Description,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc);

public sealed record MilestoneMutationResult(
    bool Success,
    Guid MilestoneId,
    Guid ProjectId,
    Guid OrganizationId);
