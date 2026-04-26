using System.Text.Json.Serialization;

namespace TaskFlow.Infrastructure.Webhooks;

internal sealed record WebhookEnvelope(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("event")] string Event,
    [property: JsonPropertyName("occurredAt")] DateTimeOffset OccurredAt,
    [property: JsonPropertyName("organizationId")] Guid OrganizationId,
    [property: JsonPropertyName("data")] object Data);
