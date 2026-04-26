namespace TaskFlow.Application.Dashboard;

public static class DashboardCacheKeys
{
    public static string DashboardStats(Guid organizationId) => $"dashboard:stats:{organizationId}";

    public static string DashboardMyStats(Guid userId) => $"dashboard:my:{userId}";
}
