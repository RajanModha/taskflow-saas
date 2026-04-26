using TaskFlow.Application.Tasks;

namespace TaskFlow.Application.Workspaces;

public sealed record TaskTemplateChecklistItemDto(Guid Id, string Title, int Order);

public sealed record TaskTemplateCreatedByDto(Guid Id, string UserName);

public sealed record TaskTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string DefaultTitle,
    string? DefaultDescription,
    TaskFlow.Domain.Entities.TaskPriority DefaultPriority,
    int? DefaultDueDaysFromNow,
    IReadOnlyList<TaskTemplateChecklistItemDto> ChecklistItems,
    IReadOnlyList<TagDto> Tags,
    TaskTemplateCreatedByDto CreatedBy,
    DateTime CreatedAtUtc);

public sealed record CreateTaskTemplateRequest(
    string Name,
    string? Description,
    string DefaultTitle,
    string? DefaultDescription,
    TaskFlow.Domain.Entities.TaskPriority DefaultPriority,
    int? DefaultDueDaysFromNow,
    IReadOnlyList<string> ChecklistItems,
    IReadOnlyList<Guid> TagIds);

public sealed record UpdateTaskTemplateRequest(
    string Name,
    string? Description,
    string DefaultTitle,
    string? DefaultDescription,
    TaskFlow.Domain.Entities.TaskPriority DefaultPriority,
    int? DefaultDueDaysFromNow,
    IReadOnlyList<string> ChecklistItems,
    IReadOnlyList<Guid> TagIds);

public interface IWorkspaceTaskTemplateService
{
    Task<IReadOnlyList<TaskTemplateDto>?> ListTemplatesAsync(Guid actorUserId, CancellationToken cancellationToken = default);
    Task<TaskTemplateDto?> GetTemplateAsync(Guid actorUserId, Guid templateId, CancellationToken cancellationToken = default);
    Task<(int StatusCode, object? Body)> CreateTemplateAsync(Guid actorUserId, CreateTaskTemplateRequest request, CancellationToken cancellationToken = default);
    Task<(int StatusCode, object? Body)> UpdateTemplateAsync(Guid actorUserId, Guid templateId, UpdateTaskTemplateRequest request, CancellationToken cancellationToken = default);
    Task<int> DeleteTemplateAsync(Guid actorUserId, Guid templateId, CancellationToken cancellationToken = default);
}
