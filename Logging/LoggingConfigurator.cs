using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Logging;

public static class LoggingConfigurator
{
    public static void ConfigureLogging(IHostBuilder hostBuilder, IConfiguration configuration)
    {
        var rootPath = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
        var logFilePath = Path.Combine(rootPath!, "AppLogs/Logs", "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration) // Load from appsettings.json if available
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .WriteTo.File(
                path: logFilePath,              // Create `Logs` folder at runtime
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,          // Keep logs for 7 days
                fileSizeLimitBytes: 10_000_000,     // 10 MB max per file
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1) // Optional but useful
             )
            .CreateLogger();

        hostBuilder.UseSerilog(); // Important: hook into .NET Host logging
    }
}
