using HttpResults = Microsoft.AspNetCore.Http.Results;  // avoid namespace clash with EndPoints.Results

namespace EndPoints.Results;

public static class ApiResults
{
    public static IResult Ok<T>(T data, string message, HttpContext ctx) =>
        // Use Ok() for success (no ambiguity, 200 status)
        HttpResults.Ok(ApiResponseFactory.Success(data, message, ctx.TraceIdentifier));

    public static IResult CreatedAt<T>(string location, T data, string message, HttpContext ctx) =>
        // Use Created() for 201
        HttpResults.Created(location, ApiResponseFactory.Created(data, message, ctx.TraceIdentifier));

    public static IResult Fail(string message, IEnumerable<string> errors, int statusCode, HttpContext ctx) =>
        // Disambiguate Json(...) by casting the 2nd param to JsonSerializerOptions?
        HttpResults.Json(
            ApiResponseFactory.Fail(message, errors.ToList(), statusCode, ctx.TraceIdentifier),
            (JsonSerializerOptions?)null, contentType: null, statusCode: statusCode
        );
}
