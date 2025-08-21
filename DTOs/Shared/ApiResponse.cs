namespace DTOs.Shared;

public record ApiResponse<T>(
    string Status,
    int HttpStatusCode,
    string Message,
    T? Data,
    List<string> Errors,
    string CorrelationId,
    DateTime Timestamp
);
