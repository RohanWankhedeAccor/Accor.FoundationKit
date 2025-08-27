// UnitTests/TestKit/Http/HttpAssertions.cs
#nullable enable
namespace UnitTests.TestKit.Http;

using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

public static class HttpAssertions
{
    public static readonly JsonSerializerOptions WebJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void ShouldBeJson(this HttpResponseMessage resp, HttpStatusCode expected)
    {
        resp.StatusCode.Should().Be(expected);
        resp.Content.Headers.ContentType!.ToString().Should().StartWith("application/json");
    }

    public static void ShouldBeProblem(this HttpResponseMessage resp, HttpStatusCode expected)
    {
        resp.StatusCode.Should().Be(expected);
        resp.Content.Headers.ContentType!.ToString().Should().StartWith("application/problem+json");
    }

    public static async Task<T> ReadAs<T>(this HttpResponseMessage resp) =>
        (await resp.Content.ReadFromJsonAsync<T>(WebJson))!;
}
