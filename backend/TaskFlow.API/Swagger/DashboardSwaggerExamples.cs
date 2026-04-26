using Swashbuckle.AspNetCore.Filters;
using TaskFlow.Application.Dashboard;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.API.Swagger;

public sealed class DashboardStatsExampleProvider : IExamplesProvider<DashboardStatsDto>
{
    private static readonly Guid SampleUser = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SampleProject = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SampleTask = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public DashboardStatsDto GetExamples()
    {
        var tasksByStatus = new[]
        {
            new TasksByStatusDto(nameof(DomainTaskStatus.Backlog), 4),
            new TasksByStatusDto(nameof(DomainTaskStatus.Todo), 6),
            new TasksByStatusDto(nameof(DomainTaskStatus.InProgress), 3),
            new TasksByStatusDto(nameof(DomainTaskStatus.Done), 12),
            new TasksByStatusDto(nameof(DomainTaskStatus.Cancelled), 1),
        };

        var tasksByPriority = new[]
        {
            new TasksByPriorityDto(nameof(TaskPriority.Low), 2),
            new TasksByPriorityDto(nameof(TaskPriority.Medium), 14),
            new TasksByPriorityDto(nameof(TaskPriority.High), 7),
            new TasksByPriorityDto(nameof(TaskPriority.Urgent), 3),
        };

        var velocity = new DashboardVelocityDto(CompletedLast7Days: 5, CompletedPrev7Days: 3, TrendPercent: 66.7m);

        var upcoming = new[]
        {
            new DashboardUpcomingTaskDto(
                SampleTask,
                "Ship v1 analytics",
                SampleProject,
                "Mobile app",
                DateTime.UtcNow.AddDays(2),
                TaskPriority.High,
                new TaskAssigneeDto(SampleUser, "alex", "Alex M.")),
        };

        var recent = new[]
        {
            new DashboardRecentActivityDto(
                "task.status_changed",
                "alex",
                DateTime.UtcNow.AddMinutes(-12),
                "Ship v1 analytics"),
            new DashboardRecentActivityDto(
                "project.created",
                "jamie",
                DateTime.UtcNow.AddHours(-3),
                "Mobile app"),
        };

        var projects = new[]
        {
            new DashboardProjectSummaryDto(
                SampleProject,
                "Mobile app",
                TotalTasks: 18,
                CompletedTasks: 9,
                OverdueCount: 1,
                Progress: 50.0m),
        };

        var contributors = new[]
        {
            new DashboardTopContributorDto(SampleUser, "alex", "Alex M.", 4),
        };

        return new DashboardStatsDto(
            TotalTasks: 26,
            CompletedTasks: 12,
            PendingTasks: 13,
            TasksByStatus: tasksByStatus,
            InProgressTasks: 3,
            CancelledTasks: 1,
            TasksByPriority: tasksByPriority,
            OverdueCount: 2,
            DueSoonCount: 4,
            CompletionRate: 46.2m,
            Velocity: velocity,
            UpcomingTasks: upcoming,
            RecentActivity: recent,
            ProjectSummaries: projects,
            TopContributors: contributors);
    }
}

public sealed class DashboardMyStatsExampleProvider : IExamplesProvider<DashboardMyStatsDto>
{
    public DashboardMyStatsDto GetExamples()
    {
        var byStatus = new[]
        {
            new TasksByStatusDto(nameof(DomainTaskStatus.Todo), 2),
            new TasksByStatusDto(nameof(DomainTaskStatus.InProgress), 1),
            new TasksByStatusDto(nameof(DomainTaskStatus.Done), 5),
            new TasksByStatusDto(nameof(DomainTaskStatus.Backlog), 0),
            new TasksByStatusDto(nameof(DomainTaskStatus.Cancelled), 0),
        };

        var byPriority = new[]
        {
            new TasksByPriorityDto(nameof(TaskPriority.Medium), 4),
            new TasksByPriorityDto(nameof(TaskPriority.High), 3),
            new TasksByPriorityDto(nameof(TaskPriority.Low), 1),
            new TasksByPriorityDto(nameof(TaskPriority.Urgent), 0),
        };

        var activity = new[]
        {
            new DashboardRecentActivityDto(
                "task.commented",
                "alex",
                DateTime.UtcNow.AddMinutes(-30),
                "Fix flaky board drag"),
        };

        return new DashboardMyStatsDto(
            new MyTasksSummaryDto(Total: 8, Completed: 5, Overdue: 1, DueSoon: 2),
            byStatus,
            byPriority,
            activity);
    }
}
