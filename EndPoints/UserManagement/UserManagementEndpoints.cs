// Web/Endpoints/UserManagementEndpoints.cs
using EndPoints.Results;                   // ApiResults.Ok/CreatedAt/NotFound
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Web.Endpoints.Users;

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
                PageSize = Math.Clamp(q.PageSize, 1, 200), // clamp only (paging validation tests later)
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
            try
            {
                var user = await svc.GetAsync(id, ct);
                if (user is null)
                    return ApiResults.NotFound("User not found", http);

                return ApiResults.Ok(user, "User retrieved successfully.", http);
            }
            catch (KeyNotFoundException)
            {
                // In case the service throws instead of returning null
                return ApiResults.NotFound("User not found", http);
            }
        })
        .WithName("GetUserById")
        .WithOpenApi()
        .WithSummary("Get user by ID")
        .WithDescription("Retrieves detailed user information using a unique identifier")
        .Produces<ApiResponse<UserDetailDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound) // envelope for 404
        .ProducesProblem(StatusCodes.Status500InternalServerError);   // unexpected errors  

        // POST /users
        group.MapPost("", async (
            [FromBody, Required] UserCreateDto dto,
            IValidator<UserCreateDto> validator,     // FluentValidation
            IUserService svc,
            HttpContext http,
            CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(dto, ct); // throws ValidationException -> 400 by your ProblemMapping

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
        group.MapPut("{id:guid}", async (
            Guid id,
            [FromBody] UserUpdateDto dto,
            IValidator<UserUpdateDto> validator,     // FluentValidation
            IUserService svc,
            HttpContext http,
            CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(dto, ct); // throws ValidationException -> 400

            try
            {
                var updated = await svc.UpdateAsync(id, dto, ct);

                if (updated is null)
                    return ApiResults.NotFound("User not found", http);

                return ApiResults.Ok(updated, "User updated successfully.", http);
            }
            catch (KeyNotFoundException)
            {
                // In case the service throws instead of returning null
                return ApiResults.NotFound("User not found", http);
            }
        })
        .WithName("UpdateUser")
        .WithOpenApi()
        .WithSummary("Update an existing user")
        .WithDescription("Updates the specified user by ID with new values")
        .Produces<ApiResponse<UserDetailDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // DELETE /users/{id}
        group.MapDelete("{id:guid}", async (Guid id, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var deleted = await svc.DeleteAsync(id, ct);
            return deleted
                ? ApiResults.Ok(true, "User deleted successfully.", http)
                : ApiResults.NotFound("User not found", http);
        })
        .WithName("DeleteUser")
        .WithOpenApi()
        .WithSummary("Delete a user by ID")
        .WithDescription("Removes a user from the system by unique ID")
        .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Test Crash (verifies ErrorHandlerMiddleware -> ProblemDetails)
        group.MapGet("crash", (HttpContext http) =>
            Results.Problem(
                title: "Test crash endpoint",
                detail: "This simulates a failure and returns ProblemDetails with 500.",
                statusCode: StatusCodes.Status500InternalServerError
            )
        )
        .WithOpenApi()
        .WithName("CrashTest")
        .WithSummary("Crash endpoint to test error handling")
        .WithDescription("Returns a ProblemDetails payload with 500 to simulate a crash.");

        return app;
    }
}

// NOTE: These interfaces are assumed to be available via using statements in your project:
// - IUserService
// - PagedQuery, PagingRequest, PaginatedResponse<T>
// - ApiResponse<T>, ApiResults (from EndPoints.Results)
