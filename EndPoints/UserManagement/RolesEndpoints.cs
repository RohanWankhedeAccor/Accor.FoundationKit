using Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Roles;

public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roles")
                       .WithTags("Roles");

        // LIST ONLY
        group.MapGet("/", async ([FromServices] IRoleService svc, CancellationToken ct) =>
        {
            var items = await svc.ListAsync(ct);
            return Results.Ok(items);
        });

        return app;
    }
}
