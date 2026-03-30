namespace TaskFlow.Application.Common;

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount);

