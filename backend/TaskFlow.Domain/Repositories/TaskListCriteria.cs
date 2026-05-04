using DomainTaskPriority = TaskFlow.Domain.Entities.TaskPriority;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Domain.Repositories;

/// <summary>Detached read criteria for listing tasks (tenant scope comes from persistence global filters).</summary>
/// <param name="ForceEmptyResult">When true, returns no rows (e.g. assigned-to-me with no authenticated user).</param>
public sealed record TaskListCriteria(
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
    Guid? AssigneeId,
    Guid? TagId,
    Guid? MilestoneId,
    bool? IsBlocked,
    bool IncludeDeleted,
    bool DeletedOnly,
    bool ForceEmptyResult = false);
