using MediatR;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Dashboard;

public sealed record TasksByStatusDto(string Status, int Count);

public sealed record TasksByPriorityDto(string Priority, int Count);

public sealed record DashboardVelocityDto(
    int CompletedLast7Days,
    int CompletedPrev7Days,
    decimal TrendPercent);

public sealed record DashboardUpcomingTaskDto(
    Guid Id,
    string Title,
    Guid ProjectId,
    string ProjectName,
    DateTime? DueDateUtc,
    TaskPriority Priority,
    TaskAssigneeDto? Assignee);

public sealed record DashboardRecentActivityDto(
    string Action,
    string ActorName,
    DateTime OccurredAt,
    string? EntityTitle);

public sealed record DashboardProjectSummaryDto(
    Guid ProjectId,
    string ProjectName,
    int TotalTasks,
    int CompletedTasks,
    int OverdueCount,
    decimal Progress);

public sealed record DashboardTopContributorDto(
    Guid UserId,
    string UserName,
    string? DisplayName,
    int TasksCompleted);

public sealed record DashboardStatsDto(
    int TotalTasks,
    int CompletedTasks,
    int PendingTasks,
    IReadOnlyList<TasksByStatusDto> TasksByStatus,
    int InProgressTasks,
    int CancelledTasks,
    IReadOnlyList<TasksByPriorityDto> TasksByPriority,
    int OverdueCount,
    int DueSoonCount,
    decimal CompletionRate,
    DashboardVelocityDto Velocity,
    IReadOnlyList<DashboardUpcomingTaskDto> UpcomingTasks,
    IReadOnlyList<DashboardRecentActivityDto> RecentActivity,
    IReadOnlyList<DashboardProjectSummaryDto> ProjectSummaries,
    IReadOnlyList<DashboardTopContributorDto> TopContributors);

public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public sealed record MyTasksSummaryDto(int Total, int Completed, int Overdue, int DueSoon);

public sealed record DashboardMyStatsDto(
    MyTasksSummaryDto MyTasks,
    IReadOnlyList<TasksByStatusDto> MyTasksByStatus,
    IReadOnlyList<TasksByPriorityDto> MyTasksByPriority,
    IReadOnlyList<DashboardRecentActivityDto> MyRecentActivity);

public sealed record GetDashboardMyStatsQuery : IRequest<DashboardMyStatsDto>;
