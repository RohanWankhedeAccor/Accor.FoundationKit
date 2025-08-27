using Business.Interfaces;          // IUserService
using Common.DTOs.Paging;
using Common.DTOs.Users;            // UserDetailDto
using EndPoints;            // RoleItemDto
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

namespace UnitTests.Endpoints;

public class UsersEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IUserService> _users = new(MockBehavior.Strict);

    public UsersEndpointsTests(WebApplicationFactory<Program> baseFactory)
    {
        _factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            // Disable migrations/seed & any file sinks for tests
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var overrides = new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Database:SeedOnStartup"] = "false",
                    ["Serilog:WriteTo:0:Name"] = "Console",
                };
                cfg.AddInMemoryCollection(overrides);
            });

            // Replace IUserService with our mock
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserService>();
                services.AddSingleton(_users.Object);

                // If not added in app startup, uncomment the next line:
                services.AddProblemDetails();
            });
        });
    }

    [Fact]
    public async Task Get_user_by_id_returns_200_with_envelope_when_found()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var returned = new UserDetailDto(
            Id: id,
            FirstName: "Jane",
            LastName: "Doe",
            Email: "jane@example.com",
            Active: true,
            CreatedDate: now,
            UpdatedDate: null,
            Roles: new List<RoleItemDto> { new(Guid.NewGuid(), "User") }
        );

        _users.Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(returned);

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync($"/users/{id}");

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/json");

        // Assert envelope + payload
        var payload = await resp.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(200);
        payload.Errors.Should().BeEmpty();
        payload.Data.Should().NotBeNull();
        payload.Data!.Id.Should().Be(id);

        _users.Verify(s => s.GetAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_user_by_id_returns_404_envelope_when_missing()
    {
        // Arrange
        var id = Guid.NewGuid();

        _users.Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync((UserDetailDto?)null);

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync($"/users/{id}");

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/json");

        // Assert envelope
        var payload = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("error");
        payload.HttpStatusCode.Should().Be(404);
        payload.Data.Should().BeNull();
        payload.Errors.Should().NotBeNull();

        _users.Verify(s => s.GetAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crash_endpoint_returns_500()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/users/crash");

        resp.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/problem+json");
    }

    [Fact]
    public async Task Get_users_paged_returns_200_with_envelope_and_paging()
    {
        // Arrange
        var page = 2;
        var pageSize = 3;

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new PagedResult<UserListItemDto>
        {
            TotalCount = 0,
            Items = Array.Empty<UserListItemDto>()
        });

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync($"/users?Page={page}&PageSize={pageSize}");

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/json");

        // Assert envelope + paging payload
        var payload = await resp.Content.ReadFromJsonAsync<DTOs.Shared.ApiResponse<UnitTests.Helpers.PaginatedResponse<UserListItemDto>>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(200);
        payload.Errors.Should().BeEmpty();

        payload.Data.Should().NotBeNull();
        payload.Data!.Page.Should().Be(page);
        payload.Data.PageSize.Should().Be(pageSize);
        payload.Data.TotalCount.Should().Be(0);
        payload.Data.Items.Should().BeEmpty();

        _users.Verify(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task Get_users_paged_returns_items_with_envelope_and_paging()
    {
        // Arrange
        var page = 1;
        var pageSize = 2;

        var u1 = new UserListItemDto(
            Id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            FirstName: "Ada",
            LastName: "Lovelace",
            Email: "ada@example.com",
            Active: true,
            Roles: new List<RoleItemDto> { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Admin") }
        );

        var u2 = new UserListItemDto(
            Id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            FirstName: "Grace",
            LastName: "Hopper",
            Email: "grace@example.com",
            Active: false,
            Roles: new List<RoleItemDto> { new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "User") }
        );

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new PagedResult<UserListItemDto>
              {
                  TotalCount = 2,
                  Items = new List<UserListItemDto> { u1, u2 }
              });

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync($"/users?Page={page}&PageSize={pageSize}");

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/json");

        // Assert envelope + paging payload
        var payload = await resp.Content.ReadFromJsonAsync<
            DTOs.Shared.ApiResponse<UnitTests.Helpers.PaginatedResponse<UserListItemDto>>
        >(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(200);
        payload.Errors.Should().BeEmpty();

        payload.Data.Should().NotBeNull();
        payload.Data!.Page.Should().Be(page);
        payload.Data.PageSize.Should().Be(pageSize);
        payload.Data.TotalCount.Should().Be(2);
        payload.Data.Items.Should().HaveCount(2);

        var items = payload.Data.Items;
        items[0].Id.Should().Be(u1.Id);
        items[0].FirstName.Should().Be("Ada");
        items[0].LastName.Should().Be("Lovelace");
        items[0].Email.Should().Be("ada@example.com");
        items[0].Active.Should().BeTrue();
        items[0].Roles.Should().ContainSingle(r => r.Name == "Admin");

        items[1].Id.Should().Be(u2.Id);
        items[1].FirstName.Should().Be("Grace");
        items[1].LastName.Should().Be("Hopper");
        items[1].Email.Should().Be("grace@example.com");
        items[1].Active.Should().BeFalse();
        items[1].Roles.Should().ContainSingle(r => r.Name == "User");

        _users.Verify(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task Create_user_returns_201_with_envelope_and_location()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // what the service should return after creation
        var created = new UserDetailDto(
            Id: newId,
            FirstName: "Sam",
            LastName: "Carter",
            Email: "sam@example.com",
            Active: true,
            CreatedDate: now,
            UpdatedDate: null,
            Roles: new List<RoleItemDto> { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "User") }
        );

        _users.Setup(s => s.CreateAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(created);

        var client = _factory.CreateClient();

        // JSON body the endpoint expects (binder is case-insensitive)
        var req = new
        {
            firstName = "Sam",
            lastName = "Carter",
            email = "sam@example.com",
            active = true,
            roleIds = new[] { Guid.Parse("11111111-1111-1111-1111-111111111111") }
        };

        // Act
        var resp = await client.PostAsJsonAsync("/users", req);

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location.Should().NotBeNull();
        resp.Headers.Location!.ToString().Should().EndWith($"/users/{newId}");

        // Assert envelope + payload
        var payload = await resp.Content.ReadFromJsonAsync<
            DTOs.Shared.ApiResponse<UserDetailDto>
        >(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(201);
        payload.Errors.Should().BeEmpty();

        payload.Data.Should().NotBeNull();
        payload.Data!.Id.Should().Be(newId);
        payload.Data.Email.Should().Be("sam@example.com");

        _users.Verify(s => s.CreateAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_user_returns_200_with_envelope_when_found()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var updated = new UserDetailDto(
            Id: id,
            FirstName: "Updated",
            LastName: "Name",
            Email: "updated@example.com",
            Active: true,
            CreatedDate: now.AddDays(-2),
            UpdatedDate: now,
            Roles: new List<RoleItemDto> { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "User") }
        );

        _users.Setup(s => s.UpdateAsync(id, It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(updated);

        var client = _factory.CreateClient();

        var body = new
        {
            firstName = "Updated",
            lastName = "Name",
            email = "updated@example.com",
            active = true,
            roleIds = new[] { Guid.Parse("11111111-1111-1111-1111-111111111111") }
        };

        // Act
        var resp = await client.PutAsJsonAsync($"/users/{id}", body);

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/json");

        // Assert envelope + payload
        var payload = await resp.Content.ReadFromJsonAsync<
            DTOs.Shared.ApiResponse<UserDetailDto>
        >(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(200);
        payload.Errors.Should().BeEmpty();

        payload.Data.Should().NotBeNull();
        payload.Data!.Id.Should().Be(id);
        payload.Data.Email.Should().Be("updated@example.com");

        _users.Verify(s => s.UpdateAsync(id, It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_user_returns_404_envelope_when_missing()
    {
        // Arrange
        var id = Guid.NewGuid();

        _users.Setup(s => s.UpdateAsync(id, It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((UserDetailDto?)null);

        var client = _factory.CreateClient();

        var body = new
        {
            firstName = "Ghost",
            lastName = "User",
            email = "ghost@example.com",
            active = false,
            roleIds = Array.Empty<Guid>()
        };

        // Act
        var resp = await client.PutAsJsonAsync($"/users/{id}", body);

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/json");

        // Assert envelope
        var payload = await resp.Content.ReadFromJsonAsync<
            DTOs.Shared.ApiResponse<object>
        >(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("error");
        payload.HttpStatusCode.Should().Be(404);
        payload.Data.Should().BeNull();

        _users.Verify(s => s.UpdateAsync(id, It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_user_returns_200_true_when_deleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        _users.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var client = _factory.CreateClient();

        // Act
        var resp = await client.DeleteAsync($"/users/{id}");

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/json");

        // Assert envelope + payload
        var payload = await resp.Content.ReadFromJsonAsync<
            DTOs.Shared.ApiResponse<bool>
        >(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(200);
        payload.Errors.Should().BeEmpty();
        payload.Data.Should().BeTrue();

        _users.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_user_returns_404_envelope_when_missing()
    {
        // Arrange
        var id = Guid.NewGuid();
        _users.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);

        var client = _factory.CreateClient();

        // Act
        var resp = await client.DeleteAsync($"/users/{id}");

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType!.ToString()
            .Should().StartWith("application/json");

        // Assert envelope
        var payload = await resp.Content.ReadFromJsonAsync<
            DTOs.Shared.ApiResponse<object>
        >(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        payload.Should().NotBeNull();
        payload!.Status.Should().Be("error");
        payload.HttpStatusCode.Should().Be(404);
        payload.Data.Should().BeNull();

        _users.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }


}
