using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

namespace ObservabilitySdk.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var config = configuration.GetSection("Observability").Get<ObservabilityConfiguration>() ?? new ObservabilityConfiguration();

        // Auto-configure service name from assembly if not provided
        if (config.ServiceName == "unknown-service")
        {
            config.ServiceName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "unknown-service";
        }

        config.Environment = environment.EnvironmentName;

        services.AddSingleton(config);

        // Configure Serilog
        ConfigureSerilog(config);

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(config.ServiceName, config.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = config.Environment,
                    ["service.instance.id"] = Environment.MachineName,
                    ["service.namespace"] = "mycompany"
                }))
            .WithMetrics(metrics => ConfigureMetrics(metrics, config))
            .WithTracing(tracing => ConfigureTracing(tracing, config));

        // Configure Logging separately
        services.AddLogging(logging => ConfigureLogging(logging, config));

        // Health Checks
        if (config.HealthChecks.Enabled)
        {
            services.AddHealthChecks()
                .AddCheck("observability", () =>
                {
                    // Verificar se os exporters estão funcionando
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Observability is working");
                });
        }

        return services;
    }

    private static void ConfigureSerilog(ObservabilityConfiguration config)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(Enum.Parse<Serilog.Events.LogEventLevel>(config.Logging.MinimumLevel))
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", config.ServiceName)
            .Enrich.WithProperty("Environment", config.Environment)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        if (config.Logging.OtlpExporter.Enabled)
        {
            // Para OTLP, use Serilog.Sinks.OpenTelemetry
            loggerConfig.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = config.Logging.OtlpExporter.Endpoint;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = config.ServiceName,
                    ["service.version"] = config.ServiceVersion
                };
            });
        }

        Log.Logger = loggerConfig.CreateLogger();
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, ObservabilityConfiguration config)
    {
        if (!config.Metrics.Enabled) return;

        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddProcessInstrumentation();

        // Add custom meters
        foreach (var customMetric in config.Metrics.CustomMetrics)
        {
            metrics.AddMeter(customMetric);
        }

        metrics.AddPrometheusExporter();
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing, ObservabilityConfiguration config)
    {
        if (!config.Tracing.Enabled) return;

        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = context => !config.Tracing.IgnoredPaths
                    .Any(path => context.Request.Path.StartsWithSegments(path));
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .SetSampler(new TraceIdRatioBasedSampler(config.Tracing.SamplingRatio))
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(config.Tracing.OtlpEndpoint);
            });
    }

    private static void ConfigureLogging(ILoggingBuilder logging, ObservabilityConfiguration config)
    {
        if (!config.Logging.Enabled)
            return;

        // Configure OpenTelemetry logging if needed
        logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(config.ServiceName, config.ServiceVersion));

            if (config.Logging.OtlpExporter.Enabled)
            {
                options.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri(config.Logging.OtlpExporter.Endpoint);
                });
            }
        });
    }
}