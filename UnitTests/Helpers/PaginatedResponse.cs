namespace UnitTests.Helpers;

public record PaginatedResponse<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<T> Items
);
