using DTOs.Shared;
using FluentAssertions;

namespace UnitTests.Contracts;

public class ApiResponseFactoryTests
{
    [Fact]
    public void Success_wraps_payload_and_sets_200()
    {
        var ctxId = "req-123";
        var data = new { Value = 42 };

        var res = ApiResponseFactory.Success(data, "ok", ctxId);

        res.Status.Should().Be("success");
        res.HttpStatusCode.Should().Be(200);
        res.Message.Should().Be("ok");
        res.Data.Should().NotBeNull();
        res.Errors.Should().BeEmpty();
        res.CorrelationId.Should().Be(ctxId);
        res.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Created_sets_201()
    {
        var ctxId = "req-456";
        var res = ApiResponseFactory.Created(new { Id = 1 }, "created", ctxId);

        res.HttpStatusCode.Should().Be(201);
        res.Status.Should().Be("success");
    }
}
