using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Observability.Settings;
using Shared.Observability.Telemetry;
using HealthChecks.UI.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Observability.Core;

public static class ObservabilityExtensions
{
    public static IObservabilityBuilder AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new ObservabilitySettings();
        configuration.GetSection(ObservabilitySettings.SectionName).Bind(settings);
        services.AddSingleton(settings);
        services.AddSingleton<AppMetrics>();
        services.AddSingleton<AppTracer>();

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: settings.ApplicationName, serviceVersion: "1.0.0");
    
        if (settings.Loki.Enabled)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(resourceBuilder);
                    options.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri(settings.Loki.EndpointUrl));
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                }));
        }

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                if (settings.Tracing.Enabled)
                {
                    tracing.SetResourceBuilder(resourceBuilder)
                        .AddSource(settings.ApplicationName)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(o => o.Endpoint = new Uri(settings.Tracing.EndpointUrl));
                }
            })
            .WithMetrics(metrics =>
            {
                if (settings.Prometheus.Enabled)
                {
                    metrics.SetResourceBuilder(resourceBuilder)
                        .AddMeter(settings.ApplicationName)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddPrometheusExporter();
                }
            });

        if (settings.HealthChecks.Enabled)
        {
            services.AddHealthChecks();
        }

        return new ObservabilityBuilder(services);
    }
    
    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetRequiredService<ObservabilitySettings>();

        if (settings.Prometheus.Enabled)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        if (settings.HealthChecks.Enabled)
        {
            app.UseHealthChecks(settings.HealthChecks.Endpoint, new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }

        return app;
    }
}