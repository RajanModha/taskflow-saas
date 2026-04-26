using System.Text.Json;
using TaskFlow.Application.Activity;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Activity;

internal static class ActivityLogMapper
{
    public static ActivityLogDto ToDto(ActivityLog row) =>
        new(
            row.Id,
            row.Action,
            new ActivityActorDto(row.ActorId, row.ActorName),
            row.OccurredAtUtc,
            ParseMetadata(row.Metadata));

    public static IReadOnlyDictionary<string, object?>? ParseMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, object?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.TryGetInt64(out var l)
                        ? l
                        : prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.GetRawText(),
                };
            }

            return dict;
        }
        catch
        {
            return null;
        }
    }
}
