// EndPoints/Infrastructure/Errors/ProblemMapping.cs
using FluentValidation;

namespace EndPoints.Infrastructure.Errors;

public static class ProblemMapping
{
    public static (int status, string title, string code) Map(Exception ex) =>
        ex switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", "ValidationFailed"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "NotFound"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", "BadRequest"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Unauthorized"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict", "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error", "ServerError")
        };

    // Returns ProblemDetails OR ValidationProblemDetails (both derive from ProblemDetails)
    public static ProblemDetails ToProblem(Exception ex, HttpContext ctx)
    {
        // Special case: FluentValidation -> ValidationProblemDetails with error dictionary
        if (ex is ValidationException vex)
        {
            var errors = vex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var vpd = new ValidationProblemDetails(errors)
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = ctx.Request.Path,
                Type = "https://httpstatuses.com/400"
            };
            vpd.Extensions["requestId"] = ctx.TraceIdentifier;
            vpd.Extensions["code"] = "ValidationFailed";
            return vpd;
        }

        var (status, title, code) = Map(ex);
        var pd = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = ex.Message,
            Instance = ctx.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };
        pd.Extensions["requestId"] = ctx.TraceIdentifier;
        pd.Extensions["code"] = code;
        return pd;
    }
}
