namespace DTOs.Shared;

public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(T data, string message, string correlationId) =>
        new("success", 200, message, data, new(), correlationId, DateTime.UtcNow);

    public static ApiResponse<T> Created<T>(T data, string message, string correlationId) =>
        new("success", 201, message, data, new(), correlationId, DateTime.UtcNow);

    public static ApiResponse<object> Fail(string message, List<string> errors, int statusCode, string correlationId) =>
        new("error", statusCode, message, null, errors, correlationId, DateTime.UtcNow);
}
