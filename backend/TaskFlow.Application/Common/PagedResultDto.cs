namespace TaskFlow.Application.Common;

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    int From,
    int To)
{
    public static PagedResultDto<T> Create(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 1 : pageSize;
        var totalPages = totalCount <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);
        var hasPreviousPage = safePage > 1 && totalPages > 0;
        var hasNextPage = totalPages > 0 && safePage < totalPages;
        var from = totalCount == 0 ? 0 : ((safePage - 1) * safePageSize) + 1;
        var to = totalCount == 0 ? 0 : Math.Min(from + items.Count - 1, (int)totalCount);

        return new PagedResultDto<T>(
            items,
            safePage,
            safePageSize,
            totalCount,
            totalPages,
            hasPreviousPage,
            hasNextPage,
            from,
            to);
    }
}

