// Web/Endpoints/RolesEndpoints.cs
using EndPoints.Results;
using FluentValidation;

namespace Web.Endpoints.Roles;

public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roles")
                       .WithTags("Roles");

        // GET /roles — List all roles
        group.MapGet("/", async ([FromServices] IRoleService svc, HttpContext http, CancellationToken ct) =>
        {
            var items = await svc.ListAsync(ct);
            return ApiResults.Ok(items, "Roles fetched successfully.", http);
        })
        .WithName("GetAllRoles")
        .WithOpenApi()
        .WithSummary("Get all roles")
        .WithDescription("Returns a list of all available roles in the system.")
        .Produces<ApiResponse<List<RoleItemDto>>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // (Optional endpoints, ready when you need them)

        // POST /roles
        group.MapPost("/", async (
            [FromBody] RoleCreateDto dto,
            IValidator<RoleCreateDto> validator,
            IRoleService svc,
            HttpContext http,
            CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(dto, ct);
            var created = await svc.CreateAsync(dto, ct);
            return ApiResults.CreatedAt($"/roles/{created.Id}", created, "Role created successfully.", http);
        })
        .WithName("CreateRole")
        .WithOpenApi()
        .WithSummary("Create a role")
        .WithDescription("Creates a new role with the specified name.")
        .Produces<ApiResponse<RoleDetailDto>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .Accepts<RoleCreateDto>("application/json");

        // PUT /roles/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] RoleUpdateDto dto,
            IValidator<RoleUpdateDto> validator,
            IRoleService svc,
            HttpContext http,
            CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(dto, ct);
            var updated = await svc.UpdateAsync(id, dto, ct);
            return updated is not null
                ? ApiResults.Ok(updated, "Role updated successfully.", http)
                : ApiResults.NotFound("Role not found", http);
        })
        .WithName("UpdateRole")
        .WithOpenApi()
        .WithSummary("Update a role")
        .WithDescription("Updates the role name.")
        .Produces<ApiResponse<RoleDetailDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}

// NOTE: These interfaces and types are assumed available:
// - IRoleService
// - ApiResults, ApiResponse<T>
// - RoleCreateDto, RoleUpdateDto, RoleDetailDto, RoleItemDto
