using EndPoints.Results;
using System.ComponentModel.DataAnnotations;

public static class UserManagementEndpoints
{
    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users");

        // GET /users ?page, pageSize, search, sort
        group.MapGet("", async ([AsParameters] PagedQuery q, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var req = new PagingRequest
            {
                Page = q.Page,
                PageSize = Math.Clamp(q.PageSize, 1, 200),
                Search = q.Search,
                Sort = q.Sort
            };

            var result = await svc.ListPagedAsync(req, ct);

            var paginated = new PaginatedResponse<UserListItemDto>(
                Page: req.Page,
                PageSize: req.PageSize,
                TotalCount: result.TotalCount,
                Items: result.Items
            );

            return ApiResults.Ok(paginated, "Users fetched successfully.", http);
        })
        .WithName("GetPagedUsers")
        .WithOpenApi()
        .WithSummary("Get paged list of users")
        .WithDescription("Returns a paginated list of users based on optional filters")
        .Produces<ApiResponse<PaginatedResponse<UserListItemDto>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /users/{id}
        group.MapGet("{id:guid}", async (Guid id, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var user = await svc.GetAsync(id, ct);
            if (user is null) throw new KeyNotFoundException($"User {id} not found");
            return ApiResults.Ok(user, "User retrieved successfully.", http);
        })
        .WithName("GetUserById")
        .WithOpenApi()
        .WithSummary("Get user by ID")
        .WithDescription("Retrieves detailed user information using a unique identifier")
        .Produces<ApiResponse<UserDetailDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /users
        group.MapPost("", async ([FromBody, Required] UserCreateDto dto, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var created = await svc.CreateAsync(dto, ct);
            return ApiResults.CreatedAt($"/users/{created.Id}", created, "User created successfully.", http);
        })
        .WithName("CreateUser")
        .WithOpenApi()
        .WithSummary("Create a new user")
        .WithDescription("Creates a user record with roles and basic details")
        .Produces<ApiResponse<UserCreateDto>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .Accepts<UserCreateDto>("application/json");

        // PUT /users/{id}
        group.MapPut("{id:guid}", async (Guid id, [FromBody, Required] UserUpdateDto dto, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var updated = await svc.UpdateAsync(id, dto, ct);
            if (updated is null) throw new KeyNotFoundException($"User {id} not found");
            return ApiResults.Ok(updated, "User updated successfully.", http);
        })
        .WithName("UpdateUser")
        .WithOpenApi()
        .WithSummary("Update an existing user")
        .WithDescription("Updates the specified user by ID with new values")
        .Produces<ApiResponse<UserUpdateDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .Accepts<UserUpdateDto>("application/json");

        // DELETE /users/{id}
        group.MapDelete("{id:guid}", async (Guid id, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var deleted = await svc.DeleteAsync(id, ct);
            if (!deleted) throw new KeyNotFoundException($"User {id} not found");
            return ApiResults.Ok(true, "User deleted successfully.", http);
        })
        .WithName("DeleteUser")
        .WithOpenApi()
        .WithSummary("Delete a user by ID")
        .WithDescription("Removes a user from the system by unique ID")
        .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Test Crash (verifies ErrorHandlerMiddleware -> ProblemDetails)
        group.MapGet("crash", (HttpContext http) =>
        {
            throw new Exception("This is a test exception!");
        })
        .WithOpenApi()
        .WithName("CrashTest")
        .WithSummary("Crash endpoint to test global error handling")
        .WithDescription("This always throws a test exception and should be caught by ErrorHandlerMiddleware.");

        return app;
    }
}
