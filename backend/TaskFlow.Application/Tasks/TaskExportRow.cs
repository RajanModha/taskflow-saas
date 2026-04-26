namespace TaskFlow.Application.Tasks;

public sealed class TaskExportRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public string Tags { get; set; } = string.Empty;
    public DateTime? DueDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
