using AppContext;
using AppContext.Context;
using Business.Interfaces;
using Business.Services;
using Data.Repositories;
using Logging;
using Microsoft.EntityFrameworkCore;
using Web.Endpoints.Roles;
using Web.Endpoints.UserManagement;

var builder = WebApplication.CreateBuilder(args);
//Call Serilog
LoggingConfigurator.ConfigureLogging(builder.Host, builder.Configuration);


// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped(typeof(IBaseRepository<,>), typeof(EfBaseRepository<,>)); // optional if you use generic directly
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();


// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Add Health Checks
app.MapCustomHealthChecks(); //  .../health page


// Register Middlewares
app.UseMiddleware<RequestBodyLoggingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ResponseLoggingMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();

// Optional: migrate on startup (nice for dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();              // optional but handy
    await SeedData.EnsureSeedRolesAsync(db);       // <-- seed roles here
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Map endpoints
app.MapUserManagementEndpoints();
app.MapRolesEndpoints();

app.UseHttpsRedirection();

app.Run();
