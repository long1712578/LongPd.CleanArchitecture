namespace LongPd.CleanArchitecture.Application.Common;

/// <summary>
/// Paginated list wrapper for Query responses.
/// Used when endpoints return lists with pagination metadata.
/// </summary>
public sealed class PagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedList<T> Create(IEnumerable<T> source, int page, int pageSize, int totalCount)
        => new(source.ToList().AsReadOnly(), page, pageSize, totalCount);

    public static PagedList<T> Empty(int page, int pageSize)
        => new([], page, pageSize, 0);
}
