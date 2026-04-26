using MediatR;

namespace TaskFlow.Application.Search;

public sealed record SearchHitDto(
    Guid Id,
    string Type,
    string Title,
    string Snippet,
    int Score,
    object Metadata);

public sealed record SearchResultDto(
    string Query,
    int TotalResults,
    IReadOnlyList<SearchHitDto> Tasks,
    IReadOnlyList<SearchHitDto> Projects,
    IReadOnlyList<SearchHitDto> Comments);

public sealed record GetWorkspaceSearchQuery(string Query, int Limit) : IRequest<SearchResultDto>;
