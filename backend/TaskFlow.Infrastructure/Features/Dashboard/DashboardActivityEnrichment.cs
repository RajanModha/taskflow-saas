using System.Text.Json;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Dashboard;
using TaskFlow.Domain.Entities;

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

    internal static IReadOnlyList<DashboardRecentActivityDto> ToRecentActivityDtos(
        IReadOnlyList<ActivityLog> logs,
        IReadOnlyDictionary<Guid, string> taskTitles,
        IReadOnlyDictionary<Guid, string> projectNames)
    {
        if (logs.Count == 0)
        {
            return [];
        }

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
