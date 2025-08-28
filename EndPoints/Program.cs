using EndPoints.Infrastructure.StartUp;
using FluentValidation;
using Web.Endpoints.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .ConfigureAppLogging(builder.Configuration)
    .UseStrictServiceProviderValidation();

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddPlatformServices();

builder.Services.AddValidatorsFromAssemblyContaining<UserCreateDtoValidator>();

var app = builder.Build();

app.UseApplicationPipeline();
await app.MigrateAndSeedAsync();
app.MapApplicationEndpoints();

app.Run();

