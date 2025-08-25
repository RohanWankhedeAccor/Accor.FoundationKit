using EndPoints.Results;         // ApiResults.Ok(...)

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

        return app;
    }
}
