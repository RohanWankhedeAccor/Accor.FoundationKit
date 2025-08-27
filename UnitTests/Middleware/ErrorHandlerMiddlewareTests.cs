namespace UnitTests.Middleware;

using EndPoints.Middleware; // ← adjust if your namespace differs
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

public sealed class ErrorHandlerMiddlewareTests
{
    [Fact]
    public async Task Writes_problem_details_on_unknown_exception_returns_500()
    {
        // Arrange DI with ProblemDetails
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails(); // registers IProblemDetailsService
        var sp = services.BuildServiceProvider();

        var ctx = new DefaultHttpContext { RequestServices = sp };
        ctx.Response.Body = new MemoryStream();

        // The next delegate throws
        RequestDelegate next = _ => throw new InvalidOperationException("boom");

        var logger = NullLogger<ErrorHandlerMiddleware>.Instance;
        var mw = new ErrorHandlerMiddleware(next, logger);
        var problem = sp.GetRequiredService<IProblemDetailsService>();

        // Act
        await mw.Invoke(ctx, problem);

        // Assert
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        ctx.Response.ContentType.Should().StartWith("application/problem+json");

        ctx.Response.Body.Position = 0;
        var json = await new StreamReader(ctx.Response.Body, Encoding.UTF8).ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        doc.RootElement.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Does_not_throw_if_response_already_started()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        var sp = services.BuildServiceProvider();

        var ctx = new DefaultHttpContext { RequestServices = sp };
        ctx.Response.Body = new MemoryStream();
        await ctx.Response.StartAsync(); // simulate started response

        RequestDelegate next = _ => throw new InvalidOperationException("boom");

        var logger = NullLogger<ErrorHandlerMiddleware>.Instance;
        var mw = new ErrorHandlerMiddleware(next, logger);
        var problem = sp.GetRequiredService<IProblemDetailsService>();

        // Should not throw even though response started
        await mw.Invoking(m => m.Invoke(ctx, problem)).Should().NotThrowAsync();
    }
}
