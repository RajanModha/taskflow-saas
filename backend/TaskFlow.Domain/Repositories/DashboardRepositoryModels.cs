using TaskFlow.Domain.Entities;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Domain.Repositories;

public sealed record DashboardStatusPriorityCountReadModel(DomainTaskStatus Status, TaskPriority Priority, int Count);

public sealed record DashboardUpcomingTaskReadModel(
    Guid Id,
    string Title,
    Guid ProjectId,
    string ProjectName,
    DateTime? DueDateUtc,
    TaskPriority Priority,
    Guid? AssigneeId,
    string? AssigneeUserName,
    string? AssigneeDisplayName);

public sealed record DashboardProjectSummaryReadModel(
    Guid ProjectId,
    string ProjectName,
    int TotalTasks,
    int CompletedTasks,
    int OverdueCount);

public sealed record DashboardTopContributorReadModel(
    Guid UserId,
    string UserName,
    string? DisplayName,
    int TasksCompleted);

public sealed record DashboardRecentActivityReadModel(
    string Action,
    string ActorName,
    DateTime OccurredAt,
    string? EntityTitle);
