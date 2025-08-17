namespace Common.DTOs;

/// <summary>Generic paging/search/sort input for list endpoints.</summary>
public class PagedQuery
{
    /// <summary>1-based page number</summary>
    public int Page { get; set; } = 1;

    /// <summary>page size (endpoint will cap)</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>optional free-text search</summary>
    public string? Search { get; set; }

    /// <summary>field name; prefix with '-' for desc. ex: "firstName", "-createdDate"</summary>
    public string? Sort { get; set; }
}
