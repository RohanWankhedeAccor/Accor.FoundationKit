using Serilog;
using System.Text;

public class RequestBodyLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Serilog.ILogger _logger;

    public RequestBodyLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
        _logger = Log.ForContext<RequestBodyLoggingMiddleware>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering(); // allows us to read the body without consuming it

        var request = context.Request;
        var method = request.Method;
        var path = request.Path + request.QueryString;

        string body = string.Empty;

        if (request.ContentLength > 0 && request.ContentType?.Contains("application/json") == true)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // reset for downstream middleware
        }

        _logger.Information(" HTTP {Method} {Path} | Body: {RequestBody} | CorrelationId: {CorrelationId}",
            method, path, Truncate(body), context.TraceIdentifier);

        await _next(context);
    }

    private string Truncate(string value, int maxLength = 1000)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "[empty]";
        return value.Length > maxLength ? value.Substring(0, maxLength) + "..." : value;
    }
}
