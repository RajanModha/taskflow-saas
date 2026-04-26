namespace TaskFlow.Application.Tasks;

public sealed record CreateTaskFromTemplateOverrides(
    string? Title,
    string? Description,
    TaskFlow.Domain.Entities.TaskPriority? Priority,
    DateTime? DueDateUtc,
    Guid? AssigneeId);

public sealed record CreateTaskFromTemplateCommand(
    Guid TemplateId,
    Guid ProjectId,
    CreateTaskFromTemplateOverrides? Overrides) : MediatR.IRequest<TaskDto?>;
