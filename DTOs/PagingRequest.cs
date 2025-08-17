namespace Common.DTOs.Paging;

public class PagingRequest
{
    public int Page { get; set; } = 1;        // 1-based
    public int PageSize { get; set; } = 20;   // cap at endpoint or service
    public string? Search { get; set; }       // optional
    public string? Sort { get; set; }         // e.g. "firstName" or "-createdDate"
}
