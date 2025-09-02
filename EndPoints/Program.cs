using EndPoints.Infrastructure.StartUp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Infra (your existing extensions)
builder.Host
    .ConfigureAppLogging(builder.Configuration)
    .UseStrictServiceProviderValidation();

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddPlatformServices();

// Entra ID (Azure AD) authentication for API
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

var app = builder.Build();

// Pipeline
app.UseApplicationPipeline();
app.UseAuthentication();
app.UseAuthorization();

// Test endpoints
app.MapGet("/health", () => Results.Ok("ok"));

app.MapGet("/secure", (ClaimsPrincipal user) =>
{
    var name = user.Identity?.Name ?? "unknown";
    return Results.Ok(new { message = $"Hello {name}" });
}).RequireAuthorization();

await app.MigrateAndSeedAsync();
app.MapApplicationEndpoints();

app.Run();
