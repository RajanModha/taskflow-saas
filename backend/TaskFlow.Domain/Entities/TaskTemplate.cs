namespace TaskFlow.Domain.Entities;

public sealed class TaskTemplate
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultTitle { get; set; } = string.Empty;
    public string? DefaultDescription { get; set; }
    public TaskPriority DefaultPriority { get; set; }
    public int? DefaultDueDaysFromNow { get; set; }
    public Guid CreatedByUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organization? Organization { get; set; }
    public ICollection<TaskTemplateChecklistItem> ChecklistItems { get; set; } = new List<TaskTemplateChecklistItem>();
    public ICollection<TaskTemplateTag> TaskTemplateTags { get; set; } = new List<TaskTemplateTag>();
}
