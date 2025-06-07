using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ObservabilitySdk.Observability;

namespace ObservabilitySdk.AspNet;


public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        var config = app.ApplicationServices.GetRequiredService<ObservabilityConfiguration>();
        
        // Prometheus metrics endpoint
        if (config.Metrics.Enabled)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint(config.Metrics.PrometheusEndpoint);
        }
        
        // Health checks
        if (config.HealthChecks.Enabled)
        {
            app.UseHealthChecks(config.HealthChecks.Endpoint);
        }
        
        // Custom middleware
        app.UseMiddleware<ObservabilityMiddleware>();
        
        return app;
    }
}