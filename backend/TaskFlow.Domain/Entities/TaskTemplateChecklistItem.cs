namespace TaskFlow.Domain.Entities;

public sealed class TaskTemplateChecklistItem
{
    public Guid Id { get; init; }
    public Guid TemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }

    public TaskTemplate Template { get; set; } = null!;
}
