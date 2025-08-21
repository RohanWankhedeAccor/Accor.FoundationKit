


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

            return Results.Ok(ApiResponseFactory.Success(
                paginated,
                "Users fetched successfully.",
                http.TraceIdentifier
            ));
        })
        .WithName("GetPagedUsers")
        .WithOpenApi()
        .WithSummary("Get paged list of users")
        .WithDescription("Returns a paginated list of users based on optional filters")
        .Produces<ApiResponse<PaginatedResponse<UserListItemDto>>>(StatusCodes.Status200OK);

        // GET /users/{id}
        group.MapGet("{id:guid}", async (Guid id, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var user = await svc.GetAsync(id, ct);

            return user is null
                ? Results.NotFound(ApiResponseFactory.Fail("User not found", new List<string>(), 404, http.TraceIdentifier))
                : Results.Ok(ApiResponseFactory.Success(user, "User retrieved successfully.", http.TraceIdentifier));
        })
        .WithName("GetUserById")
        .WithOpenApi()
        .WithSummary("Get user by ID")
        .WithDescription("Retrieves detailed user information using a unique identifier")
        .Produces<ApiResponse<UserDetailDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        // POST /users
        group.MapPost("", async ([FromBody] UserCreateDto dto, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var created = await svc.CreateAsync(dto, ct);

            return Results.Created($"/users/{created.Id}",
                ApiResponseFactory.Success(created, "User created successfully.", http.TraceIdentifier));
        })
        .WithName("CreateUser")
        .WithOpenApi()
        .WithSummary("Create a new user")
        .WithDescription("Creates a user record with roles and basic details")
        .Produces<ApiResponse<UserCreateDto>>(StatusCodes.Status201Created)
        .Accepts<UserCreateDto>("application/json");

        // PUT /users/{id}
        group.MapPut("{id:guid}", async (Guid id, [FromBody] UserUpdateDto dto, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var updated = await svc.UpdateAsync(id, dto, ct);

            return updated is null
                ? Results.NotFound(ApiResponseFactory.Fail("User not found", new List<string>(), 404, http.TraceIdentifier))
                : Results.Ok(ApiResponseFactory.Success(updated, "User updated successfully.", http.TraceIdentifier));
        })
        .WithName("UpdateUser")
        .WithOpenApi()
        .WithSummary("Update an existing user")
        .WithDescription("Updates the specified user by ID with new values")
        .Produces<ApiResponse<UserUpdateDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .Accepts<UserUpdateDto>("application/json");

        // DELETE /users/{id}
        group.MapDelete("{id:guid}", async (Guid id, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var deleted = await svc.DeleteAsync(id, ct);

            return deleted
                ? Results.Ok(ApiResponseFactory.Success(true, "User deleted successfully.", http.TraceIdentifier))
                : Results.NotFound(ApiResponseFactory.Fail("User not found", new List<string>(), 404, http.TraceIdentifier));
        })
        .WithName("DeleteUser")
        .WithOpenApi()
        .WithSummary("Delete a user by ID")
        .WithDescription("Removes a user from the system by unique ID")
        .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        // Test Crash
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
