using TaskFlow.Application.Common;

namespace TaskFlow.Application.Workspaces;

public sealed record WebhookDto(
    Guid Id,
    string Url,
    IReadOnlyList<string> Events,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record WebhookDeliveryLogDto(
    Guid Id,
    string EventType,
    string Status,
    int AttemptCount,
    DateTime? LastAttemptAt,
    int? ResponseStatus);

public sealed record CreateWorkspaceWebhookRequest(string Url, IReadOnlyList<string> Events, string Secret);

public sealed record UpdateWorkspaceWebhookRequest(
    string? Url,
    IReadOnlyList<string>? Events,
    bool? IsActive,
    string? Secret);

public sealed record WebhookTestResponse(bool Delivered, int? ResponseStatus);
