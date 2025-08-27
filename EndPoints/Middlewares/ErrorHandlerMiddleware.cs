namespace EndPoints.Middleware;
public sealed class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _log;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task Invoke(HttpContext ctx, IProblemDetailsService problemDetails)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception");

            if (ctx.Response.HasStarted)
            {
                _log.LogWarning("Response already started; skipping problem write.");
                return;
            }

            try
            {
                ctx.Response.Clear();
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await problemDetails.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = ctx,
                    Exception = ex
                });
            }
            catch (ObjectDisposedException ode)
            {
                _log.LogWarning(ode, "Response body disposed while writing problem details.");
            }
        }
    }
}
