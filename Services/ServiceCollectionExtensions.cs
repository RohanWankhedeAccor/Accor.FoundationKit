
namespace Business.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers domain/business services.
    /// </summary>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        return services;
    }
}
