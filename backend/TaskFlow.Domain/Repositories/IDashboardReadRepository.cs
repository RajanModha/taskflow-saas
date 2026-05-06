namespace TaskFlow.Domain.Repositories;

public interface IDashboardReadRepository
{
    Task<IReadOnlyList<DashboardStatusPriorityCountReadModel>> GetStatusPriorityCountsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardStatusPriorityCountReadModel>> GetMyStatusPriorityCountsAsync(Guid userId, CancellationToken cancellationToken);
    Task<int> GetOverdueCountAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> GetMyOverdueCountAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken);
    Task<int> GetDueSoonCountAsync(DateTime nowUtc, DateTime dueSoonEndUtc, CancellationToken cancellationToken);
    Task<int> GetMyDueSoonCountAsync(Guid userId, DateTime nowUtc, DateTime dueSoonEndUtc, CancellationToken cancellationToken);
    Task<int> GetCompletedCountInRangeAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardUpcomingTaskReadModel>> GetUpcomingTasksAsync(DateTime nowUtc, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardProjectSummaryReadModel>> GetProjectSummariesAsync(DateTime nowUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardTopContributorReadModel>> GetTopContributorsAsync(Guid organizationId, DateTime monthStartUtc, DateTime monthEndUtc, string statusChangedAction, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardRecentActivityReadModel>> GetRecentActivityAsync(int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardRecentActivityReadModel>> GetMyRecentActivityAsync(Guid userId, int limit, CancellationToken cancellationToken);
}
