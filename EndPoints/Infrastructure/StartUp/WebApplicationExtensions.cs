
using AppContext;
using AppContext.Context;
using EndPoints.Middleware;
using global::Web.Endpoints.Roles;
using Microsoft.EntityFrameworkCore;
using Web.Endpoints.Users;

namespace EndPoints.Infrastructure.StartUp;

public static class WebApplicationExtensions
{
    /// Common HTTP pipeline for the app.
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();

        // ✅ In tests: keep the pipeline minimal. No custom middlewares that wrap/replace streams.
        if (app.Environment.IsEnvironment("Testing"))
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            return app; // <— EARLY RETURN: nothing else for Testing
        }

        // Normal pipeline for non-test
        app.UseMiddleware<ErrorHandlerMiddleware>();
        app.UseMiddleware<RequestBodyLoggingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ResponseLoggingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    /// Maps health and feature endpoints.
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        app.MapCustomHealthChecks();    // .../health
        app.MapUserManagementEndpoints();
        app.MapRolesEndpoints();
        return app;
    }

    /// Runs EF migrations and seeds (config-guarded).
    public static async Task<WebApplication> MigrateAndSeedAsync(this WebApplication app)
    {
        if (!ShouldRunMigrations(app.Configuration)) return app;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
        await SeedData.EnsureSeedRolesAsync(db, app.Lifetime.ApplicationStopping);

        return app;
    }

    private static bool ShouldRunMigrations(IConfiguration config)
        => config.GetValue<bool>("Database:MigrateOnStartup", true);
}
