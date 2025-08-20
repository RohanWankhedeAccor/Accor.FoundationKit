using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

public static class HealthCheckExtensions
{
    public static void MapCustomHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description
                    }),
                    duration = report.TotalDuration.TotalMilliseconds
                });

                await context.Response.WriteAsync(result);
            }
        });
    }
}
