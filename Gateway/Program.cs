using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Configure token validation + logging
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.ValidateAudience = false;

    options.Authority = $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0";

    options.TokenValidationParameters.ValidAudiences = new[]
    {
        "https://graph.microsoft.com",
        builder.Configuration["AzureAd:ClientId"]
    };

    options.TokenValidationParameters.ValidIssuers = new[]
    {
        $"https://sts.windows.net/{builder.Configuration["AzureAd:TenantId"]}/",
        $"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/v2.0"
    };
});


builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

// ✅ Swagger: Add support for JWT Bearer Authorization
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Secure API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme,
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

Console.WriteLine(" App is starting...");

// Logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($" Incoming Request: {context.Request.Method} {context.Request.Path}");

    if (context.Request.Headers.ContainsKey("Authorization"))
        Console.WriteLine(" Authorization Header: " + context.Request.Headers["Authorization"]);
    else
        Console.WriteLine("⚠️ No Authorization header");

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Public endpoint
app.MapGet("/public", () => "This is a public endpoint.")
   .AllowAnonymous();

// Secure endpoint — returns claims
app.MapGet("/secure", (HttpContext context) =>
{
    var claims = context.User.Claims
        .Select(c => new { Type = c.Type, Value = c.Value })
        .ToList();

    return Results.Ok(new
    {
        Message = "You are authenticated!",
        User = context.User.Identity?.Name ?? "Unknown",
        Claims = claims
    });
}).RequireAuthorization();

app.Run();
