namespace Common.DTOs.Paging;

public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public PagedResult() { }

    public PagedResult(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        Items = items.ToList();
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
