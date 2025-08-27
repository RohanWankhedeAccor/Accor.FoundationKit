// UnitTests/Endpoints/UsersEndpointsTests.cs
#nullable enable
namespace UnitTests.Endpoints;

using Business.Interfaces;                 // IUserService
using Common.DTOs.Paging;                 // PagingRequest, PagedResult<T>, PaginatedResponse<T>
using Common.DTOs.Users;                  // User* DTOs, RoleItemDto
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
using UnitTests.TestKit.Http;             // HttpAssertions (ShouldBeJson/Problem, ReadAs)
using static UnitTests.TestKit.Builders.UsersDtoBuilder;

public sealed class UsersEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
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

            // Replace IUserService with our mock + ensure ProblemDetails is registered
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserService>();
                services.AddSingleton(_users.Object);
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

        var returned = MakeDetail(
            id, "Jane", "Doe", "jane@example.com", true,
            created: now, updated: null,
            roles: new() { new(Guid.NewGuid(), "User") }
        );

        _users.Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(returned);

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync($"/users/{id}");

        // Assert
        resp.ShouldBeJson(HttpStatusCode.OK);
        var payload = await resp.ReadAs<ApiResponse<UserDetailDto>>();
        payload.Status.Should().Be("success");
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

        // Assert
        resp.ShouldBeJson(HttpStatusCode.NotFound);
        var payload = await resp.ReadAs<ApiResponse<object>>();
        payload.Status.Should().Be("error");
        payload.HttpStatusCode.Should().Be(404);
        payload.Data.Should().BeNull();

        _users.Verify(s => s.GetAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crash_endpoint_returns_500_problem_details()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/users/crash");
        resp.ShouldBeProblem(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Get_users_paged_returns_200_with_envelope_and_paging()
    {
        // Arrange
        var page = 2;
        var size = 3;

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new PagedResult<UserListItemDto>
              {
                  TotalCount = 0,
                  Items = Array.Empty<UserListItemDto>()
              });

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync($"/users?Page={page}&PageSize={size}");

        // Assert
        resp.ShouldBeJson(HttpStatusCode.OK);
        var payload = await resp.ReadAs<ApiResponse<PaginatedResponse<UserListItemDto>>>();
        payload.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(200);
        payload.Errors.Should().BeEmpty();
        payload.Data!.Page.Should().Be(page);
        payload.Data.PageSize.Should().Be(size);
        payload.Data.TotalCount.Should().Be(0);
        payload.Data.Items.Should().BeEmpty();

        _users.Verify(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_users_paged_returns_items_with_envelope_and_paging()
    {
        // Arrange
        var page = 1;
        var size = 2;

        var u1 = MakeListItem(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                              "Ada", "Lovelace", "ada@example.com", true,
                              new() { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Admin") });

        var u2 = MakeListItem(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                              "Grace", "Hopper", "grace@example.com", false,
                              new() { new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "User") });

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new PagedResult<UserListItemDto>
              {
                  TotalCount = 2,
                  Items = new List<UserListItemDto> { u1, u2 }
              });

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync($"/users?Page={page}&PageSize={size}");

        // Assert
        resp.ShouldBeJson(HttpStatusCode.OK);
        var payload = await resp.ReadAs<ApiResponse<PaginatedResponse<UserListItemDto>>>();
        payload.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(200);
        payload.Errors.Should().BeEmpty();
        payload.Data!.Page.Should().Be(page);
        payload.Data.PageSize.Should().Be(size);
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

        var created = MakeDetail(
            newId, "Sam", "Carter", "sam@example.com", true,
            created: now, updated: null,
            roles: new() { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "User") }
        );

        _users.Setup(s => s.CreateAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(created);

        var client = _factory.CreateClient();

        var body = new
        {
            firstName = "Sam",
            lastName = "Carter",
            email = "sam@example.com",
            active = true,
            roleIds = new[] { Guid.Parse("11111111-1111-1111-1111-111111111111") }
        };

        // Act
        var resp = await client.PostAsJsonAsync("/users", body);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location.Should().NotBeNull();
        resp.Headers.Location!.ToString().Should().EndWith($"/users/{newId}");

        var payload = await resp.ReadAs<ApiResponse<UserDetailDto>>();
        payload.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(201);
        payload.Errors.Should().BeEmpty();
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

        var updated = MakeDetail(
            id, "Updated", "Name", "updated@example.com", true,
            created: now.AddDays(-2), updated: now,
            roles: new() { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "User") }
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

        // Assert
        resp.ShouldBeJson(HttpStatusCode.OK);
        var payload = await resp.ReadAs<ApiResponse<UserDetailDto>>();
        payload.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(200);
        payload.Errors.Should().BeEmpty();
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

        // Assert
        resp.ShouldBeJson(HttpStatusCode.NotFound);
        var payload = await resp.ReadAs<ApiResponse<object>>();
        payload.Status.Should().Be("error");
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

        // Assert
        resp.ShouldBeJson(HttpStatusCode.OK);
        var payload = await resp.ReadAs<ApiResponse<bool>>();
        payload.Status.Should().Be("success");
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

        // Assert
        resp.ShouldBeJson(HttpStatusCode.NotFound);
        var payload = await resp.ReadAs<ApiResponse<object>>();
        payload.Status.Should().Be("error");
        payload.HttpStatusCode.Should().Be(404);
        payload.Data.Should().BeNull();

        _users.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_users_paged_clamps_page_size_above_max_to_200()
    {
        // Arrange
        PagingRequest? captured = null;

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .Callback<PagingRequest, CancellationToken>((req, _) => captured = req)
              .ReturnsAsync(new PagedResult<UserListItemDto>
              {
                  TotalCount = 0,
                  Items = Array.Empty<UserListItemDto>()
              });

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync("/users?Page=1&PageSize=9999");

        // Assert
        resp.ShouldBeJson(HttpStatusCode.OK);
        var payload = await resp.ReadAs<ApiResponse<PaginatedResponse<UserListItemDto>>>();
        payload.Data!.PageSize.Should().Be(200);
        captured!.PageSize.Should().Be(200);
    }

    [Fact]
    public async Task Get_users_paged_clamps_page_size_below_min_to_1()
    {
        // Arrange
        PagingRequest? captured = null;

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .Callback<PagingRequest, CancellationToken>((req, _) => captured = req)
              .ReturnsAsync(new PagedResult<UserListItemDto>
              {
                  TotalCount = 0,
                  Items = Array.Empty<UserListItemDto>()
              });

        var client = _factory.CreateClient();

        // Act
        var resp = await client.GetAsync("/users?Page=3&PageSize=0");

        // Assert
        resp.ShouldBeJson(HttpStatusCode.OK);
        var payload = await resp.ReadAs<ApiResponse<PaginatedResponse<UserListItemDto>>>();
        payload.Data!.PageSize.Should().Be(1);
        captured!.PageSize.Should().Be(1);
    }
}
