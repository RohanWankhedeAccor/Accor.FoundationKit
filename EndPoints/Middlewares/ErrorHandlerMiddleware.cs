using EndPoints.Infrastructure.Errors;

namespace EndPoints.Middleware;

public sealed class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    public ErrorHandlerMiddleware(RequestDelegate next) => _next = next;

    // Note: the middleware Invoke can receive DI services as extra parameters
    public async Task Invoke(HttpContext context, IProblemDetailsService? problemDetailsService = null)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var pd = ProblemMapping.ToProblem(ex, context);

            context.Response.Clear();
            context.Response.StatusCode = pd.Status ?? StatusCodes.Status500InternalServerError;

            if (problemDetailsService is not null)
            {
                // Writes with "application/problem+json"
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = pd
                });
            }
            else
            {
                // Fallback: still use the correct content type
                context.Response.ContentType = "application/problem+json";
                var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                await context.Response.WriteAsJsonAsync(pd, jsonOpts, "application/problem+json");
            }
        }
    }
}
