using DomainTaskPriority = TaskFlow.Domain.Entities.TaskPriority;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Domain.Repositories;

public sealed record TaskExportFilters(
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
    bool IncludeDeleted);
