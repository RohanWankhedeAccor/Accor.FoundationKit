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
        // Built-in ProblemDetails (no IncludeExceptionDetails here)
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                // Add correlation/trace id to every problem
                ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.TraceIdentifier;

                // (optional) in Development, include a tiny hint (avoid full stack)
                var env = ctx.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                if (env.IsDevelopment() && ctx.Exception is not null)
                {
                    ctx.ProblemDetails.Extensions["error"] = ctx.Exception.Message;
                }
            };
        });

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
