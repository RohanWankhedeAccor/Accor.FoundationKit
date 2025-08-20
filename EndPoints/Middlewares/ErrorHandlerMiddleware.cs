using DTOs.Shared;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // Pass request to next middleware
        }
        catch (Exception ex)
        {
            var correlationId = context.TraceIdentifier;
            var statusCode = 500;
            var message = "An unexpected error occurred.";

            _logger.LogError(ex, "Unhandled exception | CorrelationId: {CorrelationId}", correlationId);

            var errorResponse = new ApiResponse<object>(
                Status: "error",
                HttpStatusCode: statusCode,
                Message: message,
                Data: null,
                Errors: new List<string> { ex.Message }, // You can customize this to avoid internal info in prod
                CorrelationId: correlationId,
                Timestamp: DateTime.UtcNow
            );

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}
