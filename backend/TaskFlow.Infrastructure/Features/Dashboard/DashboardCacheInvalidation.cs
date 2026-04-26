using Microsoft.Extensions.Caching.Memory;
using TaskFlow.Application.Dashboard;

namespace TaskFlow.Infrastructure.Features.Dashboard;

internal static class DashboardCacheInvalidation
{
    internal static void InvalidateOrganizationStats(IMemoryCache cache, Guid organizationId) =>
        cache.Remove(DashboardCacheKeys.DashboardStats(organizationId));

    internal static void InvalidateMyStatsForUsers(IMemoryCache cache, params Guid?[] userIds)
    {
        foreach (var id in userIds.Where(u => u.HasValue).Select(u => u!.Value).Distinct())
        {
            cache.Remove(DashboardCacheKeys.DashboardMyStats(id));
        }
    }

    internal static void InvalidateAfterTaskMutation(
        IMemoryCache cache,
        Guid organizationId,
        Guid? actorId,
        Guid? previousAssigneeId,
        Guid? newAssigneeId)
    {
        InvalidateOrganizationStats(cache, organizationId);
        InvalidateMyStatsForUsers(cache, actorId, previousAssigneeId, newAssigneeId);
    }
}
