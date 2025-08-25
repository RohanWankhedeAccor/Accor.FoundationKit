namespace EndPoints.Infrastructure.Errors;

public static class ProblemMapping
{
    public static (int status, string title, string code) Map(Exception ex) =>
        ex switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "NotFound"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", "BadRequest"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Unauthorized"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict", "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error", "ServerError")
        };

    public static ProblemDetails ToProblem(Exception ex, HttpContext ctx)
    {
        var (status, title, code) = Map(ex);
        var pd = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = ex.Message,
            Instance = ctx.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };
        // Extensions: consistent with your envelope fields
        pd.Extensions["requestId"] = ctx.TraceIdentifier;
        pd.Extensions["code"] = code;
        return pd;
    }
}
