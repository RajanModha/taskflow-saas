using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Repositories;

public sealed record WorkspaceTemplateChecklistItemReadModel(
    Guid Id,
    string Title,
    int Order);

public sealed record WorkspaceTemplateTagReadModel(
    Guid Id,
    string Name,
    string Color);

public sealed record WorkspaceTaskTemplateReadModel(
    Guid Id,
    string Name,
    string? Description,
    string DefaultTitle,
    string? DefaultDescription,
    TaskPriority DefaultPriority,
    int? DefaultDueDaysFromNow,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAtUtc,
    IReadOnlyList<WorkspaceTemplateChecklistItemReadModel> ChecklistItems,
    IReadOnlyList<WorkspaceTemplateTagReadModel> Tags);

public sealed record WorkspaceTaskTemplateMutationInput(
    string Name,
    string? Description,
    string DefaultTitle,
    string? DefaultDescription,
    TaskPriority DefaultPriority,
    int? DefaultDueDaysFromNow,
    IReadOnlyList<string> ChecklistItems,
    IReadOnlyList<Guid> TagIds);
