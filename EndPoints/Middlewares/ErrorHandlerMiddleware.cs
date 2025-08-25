using EndPoints.Infrastructure.Errors;

public sealed class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlerMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            var pd = ProblemMapping.ToProblem(ex, ctx);
            ctx.Response.StatusCode = pd.Status ?? StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(pd);
        }
    }
}
