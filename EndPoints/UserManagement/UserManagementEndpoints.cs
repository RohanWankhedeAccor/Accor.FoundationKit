using Business.Interfaces;
using Common.DTOs;
using Common.DTOs.Paging;
using Common.DTOs.Users;
using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.UserManagement;

public static class UserManagementEndpoints
{
    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users");

        // GET /users ?page, pageSize, search, sort
        group.MapGet("", async ([AsParameters] PagedQuery q, IUserService svc, CancellationToken ct) =>
        {
            var req = new PagingRequest
            {
                Page = q.Page,
                PageSize = Math.Clamp(q.PageSize, 1, 200),
                Search = q.Search,
                Sort = q.Sort
            };

            var result = await svc.ListPagedAsync(req, ct);
            return Results.Ok(result);
        });

        // GET /users/{id}
        group.MapGet("{id:guid}", async (Guid id, IUserService svc, CancellationToken ct) =>
        {
            var dto = await svc.GetAsync(id, ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        // POST /users
        group.MapPost("", async ([FromBody] UserCreateDto dto, IUserService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateAsync(dto, ct);
            return Results.Created($"/users/{created.Id}", created);
        });

        // PUT /users/{id}
        group.MapPut("{id:guid}", async (Guid id, [FromBody] UserUpdateDto dto, IUserService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateAsync(id, dto, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        // DELETE /users/{id}
        group.MapDelete("{id:guid}", async (Guid id, IUserService svc, CancellationToken ct) =>
        {
            var ok = await svc.DeleteAsync(id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
