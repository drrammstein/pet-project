using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddSharedTelemetry(
        this IServiceCollection services,
        string serviceName)
    {
        // Настройка ресурсов OpenTelemetry
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();

        // Трассировка
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                            !httpContext.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase) &&
                            !httpContext.Request.Path.Equals("/metrics", StringComparison.OrdinalIgnoreCase);
                    })
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter()
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = "localhost";
                        options.AgentPort = 6831;
                    }));

        return services;
    }
}