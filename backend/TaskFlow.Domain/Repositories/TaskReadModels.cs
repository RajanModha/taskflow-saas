using DomainTaskStatus = TaskFlow.Domain.Entities.TaskStatus;

namespace TaskFlow.Domain.Repositories;

public sealed record TaskCommentReadModel(
    Guid Id,
    string Content,
    bool IsEdited,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool IsDeleted,
    Guid? AuthorId,
    string? AuthorUserName,
    string? AuthorDisplayName);

public sealed record TaskChecklistItemReadModel(
    Guid Id,
    string Title,
    bool IsCompleted,
    int Order,
    DateTime? CompletedAtUtc);

public sealed record TaskBlockingSummaryReadModel(Guid Id, string Title, DomainTaskStatus Status);

public sealed record TaskDependenciesReadModel(
    Guid TaskId,
    string Title,
    DomainTaskStatus Status,
    IReadOnlyList<TaskBlockingSummaryReadModel> BlockedBy,
    IReadOnlyList<TaskBlockingSummaryReadModel> Blocking);
