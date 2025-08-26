using Business.Interfaces;     // IRoleService
using Common.DTOs.Users;
using EndPoints;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

// NOTE: EndPoints project must have: public partial class Program {}
namespace UnitTests.Endpoints
{
    // Minimal copy of your envelope for deserialization in tests
    public record ApiResponse<T>(
        string Status,
        int HttpStatusCode,
        string Message,
        T? Data,
        List<string> Errors,
        string CorrelationId,
        DateTime Timestamp);

    public class RolesEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<IRoleService> _rolesMock = new(MockBehavior.Strict);

        public RolesEndpointsTests(WebApplicationFactory<Program> baseFactory)
        {
            _factory = baseFactory.WithWebHostBuilder(builder =>
            {
                // Run API in a safe test environment
                builder.UseEnvironment("Testing");

                // Disable DB migrations / seeding and any file logging in tests
                builder.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var overrides = new Dictionary<string, string?>
                    {
                        ["Database:MigrateOnStartup"] = "false",
                        // if you have a separate seed flag, disable it too:
                        ["Database:SeedOnStartup"] = "false",

                        // keep Serilog to console only (avoid file sinks)
                        ["Serilog:WriteTo:0:Name"] = "Console",
                    };
                    cfg.AddInMemoryCollection(overrides);
                });

                // Swap IRoleService with a mock
                builder.ConfigureTestServices(services =>
                {

                    // swap out the real service with a mock
                    services.RemoveAll<Business.Interfaces.IRoleService>();
                    services.AddSingleton(_rolesMock.Object);

                });
            });
        }

        [Fact]
        public async Task Get_roles_returns_200_and_api_envelope()
        {
            // Arrange
            var roles = new List<RoleItemDto>
            {
                new(Guid.NewGuid(), "Admin"),
                new(Guid.NewGuid(), "Super Admin"),
                new(Guid.NewGuid(), "User"),
                new(Guid.NewGuid(), "Viewer"),
            };
            _rolesMock.Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(roles);

            var client = _factory.CreateClient();

            // Act
            var resp = await client.GetAsync("/roles");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var payload = await resp.Content.ReadFromJsonAsync<ApiResponse<List<RoleItemDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Assert envelope + payload
            payload.Should().NotBeNull();
            payload!.Status.Should().Be("success");
            payload.HttpStatusCode.Should().Be(200);
            payload.Errors.Should().BeEmpty();
            payload.Data.Should().NotBeNull();
            payload.Data!.Count.Should().Be(roles.Count);

            _rolesMock.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Get_roles_returns_empty_list_when_service_returns_none()
        {
            // Arrange
            _rolesMock.Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<RoleItemDto>());

            var client = _factory.CreateClient();

            // Act
            var resp = await client.GetAsync("/roles");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var payload = await resp.Content.ReadFromJsonAsync<ApiResponse<List<RoleItemDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Assert
            payload.Should().NotBeNull();
            payload!.Status.Should().Be("success");
            payload.Data.Should().NotBeNull();
            payload.Data!.Should().BeEmpty();

            _rolesMock.Verify(s => s.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
