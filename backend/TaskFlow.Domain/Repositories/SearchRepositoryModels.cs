namespace TaskFlow.Domain.Repositories;

public sealed record SearchHitReadModel(
    Guid Id,
    string Type,
    string Title,
    string Snippet,
    int Score,
    object Metadata);

public sealed record SearchResultReadModel(
    string Query,
    int Total,
    IReadOnlyList<SearchHitReadModel> Tasks,
    IReadOnlyList<SearchHitReadModel> Projects,
    IReadOnlyList<SearchHitReadModel> Comments);
