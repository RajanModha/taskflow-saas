using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Dashboard;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Features.Dashboard;

internal static class DashboardActivityEnrichment
{
    internal static string? TryReadTitleFromMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            var root = doc.RootElement;
            if (root.TryGetProperty("title", out var titleEl))
            {
                return titleEl.GetString();
            }

            if (root.TryGetProperty("name", out var nameEl))
            {
                return nameEl.GetString();
            }
        }
        catch (JsonException)
        {
            // Ignore malformed metadata.
        }

        return null;
    }

    internal static async Task<IReadOnlyList<DashboardRecentActivityDto>> ToRecentActivityDtosAsync(
        TaskFlowDbContext db,
        IReadOnlyList<ActivityLog> logs,
        CancellationToken cancellationToken)
    {
        if (logs.Count == 0)
        {
            return [];
        }

        var taskIds = logs
            .Where(l => l.EntityType == ActivityEntityTypes.Task)
            .Select(l => l.EntityId)
            .Distinct()
            .ToList();

        var projectIds = logs
            .Where(l => l.EntityType == ActivityEntityTypes.Project)
            .Select(l => l.EntityId)
            .Distinct()
            .ToList();

        var taskTitles = await db.Tasks.AsNoTracking()
            .Where(t => taskIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var projectNames = await db.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var list = new List<DashboardRecentActivityDto>(logs.Count);
        foreach (var log in logs)
        {
            var fromMeta = TryReadTitleFromMetadata(log.Metadata);
            var entityTitle = fromMeta;
            if (entityTitle is null && log.EntityType == ActivityEntityTypes.Task &&
                taskTitles.TryGetValue(log.EntityId, out var taskTitle))
            {
                entityTitle = taskTitle;
            }
            else if (entityTitle is null && log.EntityType == ActivityEntityTypes.Project &&
                     projectNames.TryGetValue(log.EntityId, out var projectName))
            {
                entityTitle = projectName;
            }

            list.Add(new DashboardRecentActivityDto(log.Action, log.ActorName, log.OccurredAtUtc, entityTitle));
        }

        return list;
    }
}
