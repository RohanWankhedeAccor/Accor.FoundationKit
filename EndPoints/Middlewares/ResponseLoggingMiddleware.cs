using Serilog;

public class ResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Serilog.ILogger _logger;
    private readonly IWebHostEnvironment _env;

    public ResponseLoggingMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _logger = Log.ForContext<ResponseLoggingMiddleware>();
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context); // Run the request

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        if (_env.IsDevelopment())
        {
            _logger.Information(" HTTP {Method} {Path} | Status: {StatusCode} | Response: {ResponseBody} | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path + context.Request.QueryString,
                context.Response.StatusCode,
                LogMaskingHelper.MaskSensitiveData(responseText),
                context.TraceIdentifier);
        }

        await responseBody.CopyToAsync(originalBodyStream); // Write back to actual response
    }

}
