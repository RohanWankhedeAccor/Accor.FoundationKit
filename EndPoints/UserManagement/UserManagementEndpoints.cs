using Business.Interfaces;
using Common.DTOs;
using Common.DTOs.Paging;
using Common.DTOs.Users;
using DTOs.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.UserManagement;

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

            var result = await svc.ListPagedAsync(req, ct); // returns: TotalCount, Items (List<UserDto>)

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
        });

        //group.MapGet("", async ([AsParameters] PagedQuery q, IUserService svc, CancellationToken ct) =>
        //{
        //    var req = new PagingRequest
        //    {
        //        Page = q.Page,
        //        PageSize = Math.Clamp(q.PageSize, 1, 200),
        //        Search = q.Search,
        //        Sort = q.Sort
        //    };

        //    var result = await svc.ListPagedAsync(req, ct);
        //    return Results.Ok(result);
        //});

        // GET /users/{id}
        group.MapGet("{id:guid}", async (Guid id, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var user = await svc.GetAsync(id, ct);

            return user is null
                ? Results.NotFound(ApiResponseFactory.Fail("User not found", new List<string>(), 404, http.TraceIdentifier))
                : Results.Ok(ApiResponseFactory.Success(user, "User retrieved successfully.", http.TraceIdentifier));
        });

        // POST /users
        group.MapPost("", async ([FromBody] UserCreateDto dto, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var created = await svc.CreateAsync(dto, ct);

            return Results.Created($"/users/{created.Id}",
                ApiResponseFactory.Success(created, "User created successfully.", http.TraceIdentifier));
        });

        // PUT /users/{id}
        group.MapPut("{id:guid}", async (Guid id, [FromBody] UserUpdateDto dto, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var updated = await svc.UpdateAsync(id, dto, ct);

            return updated is null
                ? Results.NotFound(ApiResponseFactory.Fail("User not found", new List<string>(), 404, http.TraceIdentifier))
                : Results.Ok(ApiResponseFactory.Success(updated, "User updated successfully.", http.TraceIdentifier));
        });


        // DELETE /users/{id}
        group.MapDelete("{id:guid}", async (Guid id, IUserService svc, HttpContext http, CancellationToken ct) =>
        {
            var deleted = await svc.DeleteAsync(id, ct);

            return deleted
                ? Results.Ok(ApiResponseFactory.Success(true, "User deleted successfully.", http.TraceIdentifier))
                : Results.NotFound(ApiResponseFactory.Fail("User not found", new List<string>(), 404, http.TraceIdentifier));
        });


        return app;
    }
}
