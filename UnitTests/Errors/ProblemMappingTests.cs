using EndPoints.Infrastructure.Errors;   // production class under EndPoints
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace UnitTests.Errors;

public class ProblemMappingTests
{
    private static HttpContext NewHttp() => new DefaultHttpContext();

    [Fact]
    public void NotFound_exception_maps_to_404()
    {
        var ex = new KeyNotFoundException("User not found");
        var ctx = NewHttp();

        var pd = ProblemMapping.ToProblem(ex, ctx);

        pd.Status.Should().Be(StatusCodes.Status404NotFound);
        pd.Title.Should().Be("Not Found");
        pd.Detail.Should().Be("User not found");
        pd.Type.Should().Be("https://httpstatuses.com/404");
        pd.Extensions.Should().ContainKey("code").WhoseValue.Should().Be("NotFound");
        pd.Extensions.Should().ContainKey("requestId");
    }

    [Fact]
    public void Unknown_exception_maps_to_500()
    {
        var ex = new Exception("boom");
        var ctx = NewHttp();

        var pd = ProblemMapping.ToProblem(ex, ctx);

        pd.Status.Should().Be(StatusCodes.Status500InternalServerError);
        pd.Title.Should().Be("Server Error");
        pd.Detail.Should().Be("boom");
        pd.Type.Should().Be("https://httpstatuses.com/500");
    }
}
