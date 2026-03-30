using MediatR;

namespace TaskFlow.Application.Dashboard;

public sealed record TasksByStatusDto(string Status, int Count);

public sealed record DashboardStatsDto(
    int TotalTasks,
    int CompletedTasks,
    int PendingTasks,
    IReadOnlyList<TasksByStatusDto> TasksByStatus);

public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

