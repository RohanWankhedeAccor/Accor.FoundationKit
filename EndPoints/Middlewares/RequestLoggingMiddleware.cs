namespace EndPoints.Middleware;
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Serilog.ILogger _logger;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
        _logger = Log.ForContext<RequestLoggingMiddleware>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context); // Continue down pipeline
            sw.Stop();

            _logger.Information(
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed:0.000}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path + context.Request.QueryString,
                context.Response.StatusCode,
                sw.Elapsed.TotalMilliseconds,
                context.TraceIdentifier
            );
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.Error(
                ex,
                "HTTP {Method} {Path} failed after {Elapsed:0.000}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path + context.Request.QueryString,
                sw.Elapsed.TotalMilliseconds,
                context.TraceIdentifier
            );

            throw; // rethrow to trigger ErrorHandlerMiddleware
        }
    }
}
