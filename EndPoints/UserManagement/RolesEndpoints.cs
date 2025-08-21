

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
            return Results.Ok(ApiResponseFactory.Success(items, "Roles fetched successfully.", http.TraceIdentifier));
        })
        .WithName("GetAllRoles")
        .WithOpenApi()
        .WithSummary("Get all roles")
        .WithDescription("Returns a list of all available roles in the system.")
        .Produces<ApiResponse<List<RoleItemDto>>>(StatusCodes.Status200OK);

        return app;

    }

}
