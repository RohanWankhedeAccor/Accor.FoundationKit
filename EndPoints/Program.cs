using AppContext;
using AppContext.Context;
using Business.Services;
using Data.Repositories;
using Logging;
using Microsoft.EntityFrameworkCore;
using Web.Endpoints.Roles;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging first
LoggingConfigurator.ConfigureLogging(builder.Host, builder.Configuration);

// 2. Service provider validation (catches DI mistakes at build time)
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// 3. Register DbContext, Repositories & Domain services
builder.Services
    .AddRepositoriesAndDb(builder.Configuration)
    .AddDomainServices();

// 4. Platform services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.EnableAnnotations()); // keep annotations

var app = builder.Build();

// 5) HTTPS redirection should be early
app.UseHttpsRedirection();

// 6) Serilog per-request log (cheap & structured)
app.UseSerilogRequestLogging();

// 7) Error handling should wrap everything after it
app.UseMiddleware<ErrorHandlerMiddleware>();

// 8) Request/Response logging order
// Body logging (buffers) -> request meta logging -> response logging
app.UseMiddleware<RequestBodyLoggingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ResponseLoggingMiddleware>();

// 9) Health checks
app.MapCustomHealthChecks(); // .../health

// 10) Optional: migrate/seed gated by config to avoid prod surprises
if (ShouldRunMigrations(builder.Configuration))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
    await SeedData.EnsureSeedRolesAsync(db, app.Lifetime.ApplicationStopping);
}

// 11) Swagger only in Dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 12) Endpoints
app.MapUserManagementEndpoints();
app.MapRolesEndpoints();

app.Run();

// ---- local helpers ----
static bool ShouldRunMigrations(IConfiguration config)
{
    // appsettings.*: { "Database": { "MigrateOnStartup": true, "SeedOnStartup": true } }
    // You can split seeding flag if you like; here we reuse one helper for clarity.
    return config.GetValue<bool>("Database:MigrateOnStartup", true);
}
