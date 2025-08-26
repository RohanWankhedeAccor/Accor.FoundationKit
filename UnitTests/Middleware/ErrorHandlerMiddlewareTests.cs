using EndPoints.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace UnitTests.Middleware;
public class ErrorHandlerMiddlewareTests
{
    [Fact]
    public async Task Writes_problem_details_on_unknown_exception_returns_500()
    {
        RequestDelegate next = _ => throw new Exception("oops");
        var mw = new ErrorHandlerMiddleware(next);
        var ctx = new DefaultHttpContext();

        await mw.Invoke(ctx); // or InvokeAsync if that's your method name

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        ctx.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvalidOperationException_maps_to_409_conflict()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("conflict");
        var mw = new ErrorHandlerMiddleware(next);
        var ctx = new DefaultHttpContext();

        await mw.Invoke(ctx);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }
}
