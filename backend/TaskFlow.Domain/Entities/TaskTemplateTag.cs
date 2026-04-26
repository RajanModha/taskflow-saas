namespace TaskFlow.Domain.Entities;

public sealed class TaskTemplateTag
{
    public Guid TemplateId { get; set; }
    public Guid TagId { get; set; }

    public TaskTemplate Template { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
