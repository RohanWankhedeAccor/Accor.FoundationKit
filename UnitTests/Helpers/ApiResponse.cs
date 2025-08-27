namespace UnitTests.Helpers;

public record ApiResponse<T>(
    string Status,
    int HttpStatusCode,
    string Message,
    T? Data,
    System.Collections.Generic.List<string> Errors,
    string CorrelationId,
    System.DateTime Timestamp);
