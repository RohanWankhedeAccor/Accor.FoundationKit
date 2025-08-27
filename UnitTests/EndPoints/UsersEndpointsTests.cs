// File: UnitTests/Endpoints/UsersEndpointsTests.cs
#nullable enable
namespace UnitTests.Endpoints;

using Business.Interfaces;                 // IUserService
using Common.DTOs.Paging;                  // PagingRequest, PagedResult<T>
using Common.DTOs.Users;                   // *User* DTOs incl. RoleItemDto
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

public class UsersEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string BasePath = "/users";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IUserService> _users = new(MockBehavior.Strict);

    public UsersEndpointsTests(WebApplicationFactory<Program> baseFactory)
    {
        _factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

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

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserService>();
                services.AddSingleton(_users.Object);
                services.AddProblemDetails(); // needed by ErrorHandlerMiddleware
            });
        });
    }

    // ---------- GET /users/{id} ----------

    [Fact]
    public async Task Get_user_by_id_returns_200_with_envelope_when_found()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var returned = MakeUserDetail(
            id: id, first: "Jane", last: "Doe", email: "jane@example.com",
            active: true, created: now, updated: null,
            roles: new() { new(Guid.NewGuid(), "User") }
        );

        _users.Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(returned);

        // Act
        var resp = await Client().GetAsync($"{BasePath}/{id}");

        // Assert
        ShouldBeJson(resp, HttpStatusCode.OK);
        var payload = await Read<ApiResponse<UserDetailDto>>(resp);
        AssertSuccess(payload, 200);
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

        // Act
        var resp = await Client().GetAsync($"{BasePath}/{id}");

        // Assert
        ShouldBeJson(resp, HttpStatusCode.NotFound);
        var payload = await Read<ApiResponse<object>>(resp);
        AssertError(payload, 404);
        payload.Data.Should().BeNull();

        _users.Verify(s => s.GetAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- GET /users (paged) ----------

    [Fact]
    public async Task Get_users_paged_returns_200_with_envelope_and_paging()
    {
        var page = 2;
        var size = 3;

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new PagedResult<UserListItemDto>
              {
                  TotalCount = 0,
                  Items = Array.Empty<UserListItemDto>()
              });

        var resp = await Client().GetAsync($"{BasePath}?Page={page}&PageSize={size}");

        ShouldBeJson(resp, HttpStatusCode.OK);

        var payload = await Read<ApiResponse<UnitTests.Helpers.PaginatedResponse<UserListItemDto>>>(resp);
        AssertSuccess(payload, 200);

        payload.Data!.Page.Should().Be(page);
        payload.Data.PageSize.Should().Be(size);
        payload.Data.TotalCount.Should().Be(0);
        payload.Data.Items.Should().BeEmpty();

        _users.Verify(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_users_paged_returns_items_with_envelope_and_paging()
    {
        var page = 1;
        var size = 2;

        var u1 = MakeUserListItem(
            id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            first: "Ada", last: "Lovelace", email: "ada@example.com",
            active: true, roles: new() { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Admin") }
        );

        var u2 = MakeUserListItem(
            id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            first: "Grace", last: "Hopper", email: "grace@example.com",
            active: false, roles: new() { new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "User") }
        );

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new PagedResult<UserListItemDto> { TotalCount = 2, Items = new List<UserListItemDto> { u1, u2 } });

        var resp = await Client().GetAsync($"{BasePath}?Page={page}&PageSize={size}");

        ShouldBeJson(resp, HttpStatusCode.OK);

        var payload = await Read<ApiResponse<UnitTests.Helpers.PaginatedResponse<UserListItemDto>>>(resp);
        AssertSuccess(payload, 200);

        payload.Data!.Page.Should().Be(page);
        payload.Data.PageSize.Should().Be(size);
        payload.Data.TotalCount.Should().Be(2);
        payload.Data.Items.Should().HaveCount(2);

        var items = payload.Data.Items;
        items[0].FirstName.Should().Be("Ada");
        items[0].Roles.Should().ContainSingle(r => r.Name == "Admin");
        items[1].FirstName.Should().Be("Grace");
        items[1].Roles.Should().ContainSingle(r => r.Name == "User");

        _users.Verify(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_users_paged_clamps_page_size_above_max_to_200()
    {
        PagingRequest? captured = null;

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .Callback<PagingRequest, CancellationToken>((req, _) => captured = req)
              .ReturnsAsync(new PagedResult<UserListItemDto> { TotalCount = 0, Items = Array.Empty<UserListItemDto>() });

        var resp = await Client().GetAsync($"{BasePath}?Page=1&PageSize=9999");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await Read<ApiResponse<UnitTests.Helpers.PaginatedResponse<UserListItemDto>>>(resp);
        payload.Data!.PageSize.Should().Be(200);
        captured!.PageSize.Should().Be(200);
    }

    [Fact]
    public async Task Get_users_paged_clamps_page_size_below_min_to_1()
    {
        PagingRequest? captured = null;

        _users.Setup(s => s.ListPagedAsync(It.IsAny<PagingRequest>(), It.IsAny<CancellationToken>()))
              .Callback<PagingRequest, CancellationToken>((req, _) => captured = req)
              .ReturnsAsync(new PagedResult<UserListItemDto> { TotalCount = 0, Items = Array.Empty<UserListItemDto>() });

        var resp = await Client().GetAsync($"{BasePath}?Page=3&PageSize=0");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await Read<ApiResponse<UnitTests.Helpers.PaginatedResponse<UserListItemDto>>>(resp);
        payload.Data!.PageSize.Should().Be(1);
        captured!.PageSize.Should().Be(1);
    }

    // ---------- POST /users ----------

    [Fact]
    public async Task Create_user_returns_201_with_envelope_and_location()
    {
        var newId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var created = MakeUserDetail(
            id: newId, first: "Sam", last: "Carter", email: "sam@example.com",
            active: true, created: now, updated: null,
            roles: new() { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "User") }
        );

        _users.Setup(s => s.CreateAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(created);

        var body = new
        {
            firstName = "Sam",
            lastName = "Carter",
            email = "sam@example.com",
            active = true,
            roleIds = new[] { Guid.Parse("11111111-1111-1111-1111-111111111111") }
        };

        var resp = await Client().PostAsJsonAsync(BasePath, body);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location!.ToString().Should().EndWith($"/users/{newId}");

        var payload = await Read<ApiResponse<UserDetailDto>>(resp);
        AssertSuccess(payload, 201);
        payload.Data!.Id.Should().Be(newId);

        _users.Verify(s => s.CreateAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- PUT /users/{id} ----------

    [Fact]
    public async Task Update_user_returns_200_with_envelope_when_found()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var updated = MakeUserDetail(
            id: id, first: "Updated", last: "Name", email: "updated@example.com",
            active: true, created: now.AddDays(-2), updated: now,
            roles: new() { new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "User") }
        );

        _users.Setup(s => s.UpdateAsync(id, It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(updated);

        var body = new
        {
            firstName = "Updated",
            lastName = "Name",
            email = "updated@example.com",
            active = true,
            roleIds = new[] { Guid.Parse("11111111-1111-1111-1111-111111111111") }
        };

        var resp = await Client().PutAsJsonAsync($"{BasePath}/{id}", body);

        ShouldBeJson(resp, HttpStatusCode.OK);
        var payload = await Read<ApiResponse<UserDetailDto>>(resp);
        AssertSuccess(payload, 200);
        payload.Data!.Email.Should().Be("updated@example.com");

        _users.Verify(s => s.UpdateAsync(id, It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_user_returns_404_envelope_when_missing()
    {
        var id = Guid.NewGuid();

        _users.Setup(s => s.UpdateAsync(id, It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((UserDetailDto?)null);

        var body = new
        {
            firstName = "Ghost",
            lastName = "User",
            email = "ghost@example.com",
            active = false,
            roleIds = Array.Empty<Guid>()
        };

        var resp = await Client().PutAsJsonAsync($"{BasePath}/{id}", body);

        ShouldBeJson(resp, HttpStatusCode.NotFound);
        var payload = await Read<ApiResponse<object>>(resp);
        AssertError(payload, 404);

        _users.Verify(s => s.UpdateAsync(id, It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- DELETE /users/{id} ----------

    [Fact]
    public async Task Delete_user_returns_200_true_when_deleted()
    {
        var id = Guid.NewGuid();

        _users.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var resp = await Client().DeleteAsync($"{BasePath}/{id}");

        ShouldBeJson(resp, HttpStatusCode.OK);
        var payload = await Read<ApiResponse<bool>>(resp);
        AssertSuccess(payload, 200);
        payload.Data.Should().BeTrue();

        _users.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_user_returns_404_envelope_when_missing()
    {
        var id = Guid.NewGuid();

        _users.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);

        var resp = await Client().DeleteAsync($"{BasePath}/{id}");

        ShouldBeJson(resp, HttpStatusCode.NotFound);
        var payload = await Read<ApiResponse<object>>(resp);
        AssertError(payload, 404);

        _users.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Crash probe (middleware path) ----------

    [Fact]
    public async Task Crash_endpoint_returns_500()
    {
        var resp = await Client().GetAsync($"{BasePath}/crash");
        resp.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        resp.Content.Headers.ContentType!.ToString().Should().StartWith("application/problem+json");
    }

    // ----------------- Helpers -----------------

    private HttpClient Client() => _factory.CreateClient();

    private static void ShouldBeJson(HttpResponseMessage resp, HttpStatusCode expected)
    {
        resp.StatusCode.Should().Be(expected);
        resp.Content.Headers.ContentType!.ToString().Should().StartWith("application/json");
    }

    private static void AssertSuccess<T>(ApiResponse<T> payload, int http)
    {
        payload.Should().NotBeNull();
        payload.Status.Should().Be("success");
        payload.HttpStatusCode.Should().Be(http);
        payload.Errors.Should().BeEmpty();
        payload.Data.Should().NotBeNull();
    }

    private static void AssertError<T>(ApiResponse<T> payload, int http)
    {
        payload.Should().NotBeNull();
        payload.Status.Should().Be("error");
        payload.HttpStatusCode.Should().Be(http);
        payload.Data.Should().BeNull();
    }

    private static async Task<T> Read<T>(HttpResponseMessage resp) =>
        (await resp.Content.ReadFromJsonAsync<T>(Json))!;

    private static UserDetailDto MakeUserDetail(
        Guid id,
        string first,
        string last,
        string email,
        bool active,
        DateTime created,
        DateTime? updated,
        List<RoleItemDto> roles)
        => new(id, first, last, email, active, created, updated, roles);

    private static UserListItemDto MakeUserListItem(
        Guid id,
        string first,
        string last,
        string email,
        bool active,
        List<RoleItemDto> roles)
        => new(id, first, last, email, active, roles);
}
