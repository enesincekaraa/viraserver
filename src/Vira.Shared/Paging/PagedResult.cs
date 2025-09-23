namespace Vira.Shared;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

}





public sealed class PagedResultCreate<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    private PagedResultCreate(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => (Items, Page, PageSize, TotalCount) = (items, page, pageSize, totalCount);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => new(items, page, pageSize, totalCount);
}

