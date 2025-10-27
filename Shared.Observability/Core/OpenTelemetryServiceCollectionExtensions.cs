using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Observability.Settings;

namespace Shared.Observability.Core;

public static class OpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddOpenTelemetryObservability(this IServiceCollection services,
        IConfiguration configuration,
        Action<OpenTelemetrySettings>? configureOptions = null)
    {
        var optionsSection = configuration.GetSection(OpenTelemetrySettings.SectionName);
        services.Configure<OpenTelemetrySettings>(optionsSection);

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddSingleton<IValidateOptions<OpenTelemetrySettings>, OpenTelemetryOptionsValidator>();

        var options = optionsSection.Get<OpenTelemetrySettings>() ?? new OpenTelemetrySettings();
        configureOptions?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            options.ServiceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown-service";
        }

        var resourceBuilder = CreateResourceBuilder(options);

        if (options.Logging.Enabled)
        {
            services.AddLogging(builder => builder.AddOpenTelemetryLogging(options, resourceBuilder));
        }

        var otelBuilder = services.AddOpenTelemetry();

        if (options.Tracing.Enabled)
        {
            otelBuilder.WithTracing(tracing => ConfigureTracing(tracing, options, resourceBuilder));
        }

        if (options.Metrics.Enabled)
        {
            otelBuilder.WithMetrics(metrics => ConfigureMetrics(metrics, options, resourceBuilder));
        }

        // Register ActivitySource for custom tracing
        services.AddSingleton(_ => new ActivitySource(options.ServiceName));

        // Register ObservabilityMetrics for custom metrics
        services.AddSingleton(sp => new ObservabilityMetrics(options.ServiceName));

        return services;
    }

    private static ResourceBuilder CreateResourceBuilder(OpenTelemetrySettings options)
    {
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion,
                serviceNamespace: options.ServiceNamespace)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = options.Environment,
                ["service.instance.id"] = Environment.MachineName,
                ["host.name"] = Environment.MachineName
            });

        if (options.ResourceAttributes.Count != 0)
        {
            resourceBuilder.AddAttributes(options.ResourceAttributes);
        }

        return resourceBuilder;
    }

    private static void AddOpenTelemetryLogging(this ILoggingBuilder builder,
        OpenTelemetrySettings options,
        ResourceBuilder resourceBuilder)
    {
        builder.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder)
                .AddOtlpExporter(exporter => ConfigureOtlpExporterForLogs(exporter, options));

            if (options.EnableConsoleExporter)
            {
                logging.AddConsoleExporter();
            }

            logging.IncludeFormattedMessage = options.Logging.IncludeFormattedMessage;
            logging.IncludeScopes = options.Logging.IncludeScopes;
            logging.ParseStateValues = options.Logging.ParseStateValues;
        });
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing,
        OpenTelemetrySettings options,
        ResourceBuilder resourceBuilder)
    {
        tracing.SetResourceBuilder(resourceBuilder)
            .SetSampler(new TraceIdRatioBasedSampler(options.Tracing.SamplingRatio))
            .AddAspNetCoreInstrumentation(aspNetCore =>
            {
                aspNetCore.RecordException = options.Tracing.RecordExceptions;
                aspNetCore.Filter = CreateHttpFilter(options.Tracing.IgnorePaths);
                aspNetCore.EnrichWithHttpRequest = EnrichWithHttpRequest;
                aspNetCore.EnrichWithHttpResponse = EnrichWithHttpResponse;
            })
            .AddHttpClientInstrumentation(httpClient =>
            {
                httpClient.RecordException = options.Tracing.RecordExceptions;
                httpClient.FilterHttpRequestMessage = CreateHttpClientFilter(options.Tracing.IgnoreHosts);
                httpClient.EnrichWithHttpRequestMessage = EnrichWithHttpRequestMessage;
                httpClient.EnrichWithHttpResponseMessage = EnrichWithHttpResponseMessage;
            })
            .AddSource($"{options.ServiceName}.*")
            .AddOtlpExporter(exporter => ConfigureOtlpExporterForTraces(exporter, options, options.Tracing.BatchExport));

        if (options.EnableConsoleExporter)
        {
            tracing.AddConsoleExporter();
        }
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, OpenTelemetrySettings options, ResourceBuilder resourceBuilder)
    {
        metrics.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter($"{options.ServiceName}.*");

        foreach (var meter in options.Metrics.CustomMeters)
        {
            metrics.AddMeter(meter);
        }

        metrics.AddOtlpExporter(exporter => ConfigureOtlpExporterForMetrics(exporter, options));

        if (options.EnableConsoleExporter)
        {
            metrics.AddConsoleExporter();
        }
    }

    private static void ConfigureOtlpExporterForLogs(OtlpExporterOptions exporter, OpenTelemetrySettings options)
    {
        exporter.Endpoint = new Uri(options.CollectorEndpoint);
        exporter.Protocol = OtlpExportProtocol.Grpc;
        exporter.ExportProcessorType = ExportProcessorType.Batch;
    }

    private static void ConfigureOtlpExporterForTraces(OtlpExporterOptions exporter, OpenTelemetrySettings options,
        BatchExportSettings batchOptions)
    {
        exporter.Endpoint = new Uri(options.CollectorEndpoint);
        exporter.Protocol = OtlpExportProtocol.Grpc;
        exporter.ExportProcessorType = ExportProcessorType.Batch;

        exporter.BatchExportProcessorOptions = new BatchExportActivityProcessorOptions
        {
            MaxQueueSize = batchOptions.MaxQueueSize,
            ScheduledDelayMilliseconds = batchOptions.ScheduledDelayMilliseconds,
            ExporterTimeoutMilliseconds = batchOptions.ExporterTimeoutMilliseconds,
            MaxExportBatchSize = batchOptions.MaxExportBatchSize
        };
    }

    private static void ConfigureOtlpExporterForMetrics(OtlpExporterOptions exporter, OpenTelemetrySettings options)
    {
        exporter.Endpoint = new Uri(options.CollectorEndpoint);
        exporter.Protocol = OtlpExportProtocol.Grpc;
        exporter.ExportProcessorType = ExportProcessorType.Batch;
    }

    private static Func<HttpContext, bool> CreateHttpFilter(List<string> ignorePaths)
    {
        return httpContext =>
        {
            var path = httpContext.Request.Path.Value?.ToLowerInvariant();
            return !ignorePaths.Any(ignorePath => path?.Contains(ignorePath.ToLowerInvariant()) == true);
        };
    }

    private static Func<HttpRequestMessage, bool> CreateHttpClientFilter(List<string> ignoreHosts)
    {
        return request =>
        {
            var host = request.RequestUri?.Host?.ToLowerInvariant();
            return !ignoreHosts.Any(ignoreHost => host?.Contains(ignoreHost.ToLowerInvariant()) == true);
        };
    }

    private static void EnrichWithHttpRequest(Activity activity, HttpRequest httpRequest)
    {
        activity.SetTag("http.request.body.size", httpRequest.ContentLength);
        activity.SetTag("http.request.protocol", httpRequest.Protocol);
        activity.SetTag("user.id", httpRequest.HttpContext.User?.Identity?.Name);
    }

    private static void EnrichWithHttpResponse(Activity activity, HttpResponse httpResponse)
    {
        activity.SetTag("http.response.body.size", httpResponse.ContentLength);
    }

    private static void EnrichWithHttpRequestMessage(Activity activity, HttpRequestMessage httpRequestMessage)
    {
        activity.SetTag("http.client.request.body.size", httpRequestMessage.Content?.Headers?.ContentLength);
        activity.SetTag("http.client.method", httpRequestMessage.Method.Method);
    }

    private static void EnrichWithHttpResponseMessage(Activity activity, HttpResponseMessage httpResponseMessage)
    {
        activity.SetTag("http.client.response.body.size", httpResponseMessage.Content?.Headers?.ContentLength);
        activity.SetTag("http.client.status_code", (int)httpResponseMessage.StatusCode);
    }
}