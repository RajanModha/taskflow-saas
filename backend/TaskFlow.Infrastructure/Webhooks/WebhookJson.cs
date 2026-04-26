using System.Text.Json;

namespace TaskFlow.Infrastructure.Webhooks;

internal static class WebhookJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<string> DeserializeEventList(string json)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json, Options);
            return list ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string SerializeEventList(IReadOnlyList<string> events) =>
        JsonSerializer.Serialize(events.Distinct(StringComparer.Ordinal).OrderBy(x => x).ToList(), Options);

    public static bool EventListContains(string eventsJson, string eventType)
    {
        var list = DeserializeEventList(eventsJson);
        return list.Contains(eventType, StringComparer.Ordinal);
    }
}
