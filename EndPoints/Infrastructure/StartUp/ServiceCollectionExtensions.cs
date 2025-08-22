// These two namespaces point to your extension methods that live in other projects:
using Business.Services;      // .AddDomainServices()
using Data.Repositories;      // .AddRepositoriesAndDb(config)
using Microsoft.OpenApi.Models;

namespace EndPoints.Infrastructure.StartUp;

public static class ServiceCollectionExtensions
{
    /// Registers DbContext + repositories + domain services.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration config)
    {
        services
            .AddRepositoriesAndDb(config)
            .AddDomainServices();

        return services;
    }

    /// Registers platform services used by the Web API host.
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddHealthChecks();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.EnableAnnotations();
            o.SwaggerDoc("v1", new OpenApiInfo { Title = "StarterKit API", Version = "v1" });
        });

        return services;
    }
}
