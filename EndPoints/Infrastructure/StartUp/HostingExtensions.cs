using Logging;

namespace EndPoints.Infrastructure.StartUp;

public static class HostingExtensions
{
    public static IHostBuilder ConfigureAppLogging(this IHostBuilder host, IConfiguration config)
    {
        // Serilog configurator
        LoggingConfigurator.ConfigureLogging(host, config);
        return host;
    }

    public static IHostBuilder UseStrictServiceProviderValidation(this IHostBuilder host)
    {
        return host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
    }
}
