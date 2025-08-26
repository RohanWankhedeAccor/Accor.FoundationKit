using EndPoints.Infrastructure.StartUp;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .ConfigureAppLogging(builder.Configuration)
    .UseStrictServiceProviderValidation();

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddPlatformServices();

var app = builder.Build();

app.UseApplicationPipeline();
await app.MigrateAndSeedAsync();
app.MapApplicationEndpoints();

app.Run();

