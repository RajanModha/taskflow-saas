namespace TaskFlow.Application.Dashboard;

public static class DashboardCacheKeys
{
    public static string DashboardStats(Guid organizationId) => $"dashboard_stats:{organizationId}";
}

