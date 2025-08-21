

namespace Data.Repositories;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AppDbContext (pooled) + repositories.
    /// </summary>
    public static IServiceCollection AddRepositoriesAndDb(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

        // Use pooling for better throughput
        services.AddDbContextPool<AppDbContext>(options =>
            options.UseNpgsql(cs));

        // Repositories
        services.AddScoped(typeof(IBaseRepository<,>), typeof(EfBaseRepository<,>)); // generic base
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        return services;
    }
}
