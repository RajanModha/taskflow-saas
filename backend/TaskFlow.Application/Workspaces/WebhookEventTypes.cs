namespace TaskFlow.Application.Workspaces;

public static class WebhookEventTypes
{
    public const string TaskCreated = "task.created";
    public const string TaskStatusChanged = "task.status_changed";
    public const string TaskAssigned = "task.assigned";
    public const string TaskDeleted = "task.deleted";
    public const string ProjectCreated = "project.created";
    public const string ProjectDeleted = "project.deleted";
    public const string MemberJoined = "member.joined";
    public const string WebhookTest = "webhook.test";

    public static readonly IReadOnlyList<string> All =
    [
        TaskCreated,
        TaskStatusChanged,
        TaskAssigned,
        TaskDeleted,
        ProjectCreated,
        ProjectDeleted,
        MemberJoined,
    ];

    public static bool IsSupported(string eventType) =>
        All.Contains(eventType, StringComparer.Ordinal);
}
