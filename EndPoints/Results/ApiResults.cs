using HttpResults = Microsoft.AspNetCore.Http.Results;  // avoid namespace clash with EndPoints.Results

namespace EndPoints.Results;

public static class ApiResults
{
    public static IResult Ok<T>(T data, string message, HttpContext ctx) =>
        HttpResults.Ok(ApiResponseFactory.Success(data, message, ctx.TraceIdentifier));

    public static IResult CreatedAt<T>(string location, T data, string message, HttpContext ctx) =>
        HttpResults.Created(location, ApiResponseFactory.Created(data, message, ctx.TraceIdentifier));

    public static IResult Fail(string message, IEnumerable<string> errors, int statusCode, HttpContext ctx) =>
        HttpResults.Json(
            ApiResponseFactory.Fail(message, errors is null ? new List<string>() : new List<string>(errors), statusCode, ctx.TraceIdentifier),
            (JsonSerializerOptions?)null, contentType: null, statusCode: statusCode
        );

    // 🔹 Convenience wrappers (use your unified envelope + correct HTTP codes)
    public static IResult NotFound(string message, HttpContext ctx) =>
        Fail(message, errors: new List<string>(), statusCode: StatusCodes.Status404NotFound, ctx);

    public static IResult BadRequest(string message, IEnumerable<string> errors, HttpContext ctx) =>
        Fail(message, errors, StatusCodes.Status400BadRequest, ctx);

    public static IResult Conflict(string message, IEnumerable<string> errors, HttpContext ctx) =>
        Fail(message, errors, StatusCodes.Status409Conflict, ctx);

    public static IResult Unauthorized(string message, HttpContext ctx) =>
        Fail(message, errors: new List<string>(), StatusCodes.Status401Unauthorized, ctx);

    public static IResult Forbidden(string message, HttpContext ctx) =>
        Fail(message, errors: new List<string>(), StatusCodes.Status403Forbidden, ctx);
}
