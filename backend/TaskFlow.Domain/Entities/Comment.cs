namespace TaskFlow.Domain.Entities;

public sealed class Comment
{
    public Guid Id { get; init; }

    public Guid TaskId { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsEdited { get; set; }

    public bool IsDeleted { get; set; }

    public Task ParentTask { get; set; } = null!;
}
