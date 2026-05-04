namespace TaskFlow.Domain.Common;

/// <summary>Page of items with total count for offset pagination (no tracking implied by caller).</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);
