using Microsoft.Extensions.Options;

namespace Shared.Observability.Settings;

public class OpenTelemetrySettings
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; set; } = string.Empty;

    public string ServiceVersion { get; set; } = "1.0.0";

    public string ServiceNamespace { get; set; } = "production";

    public string Environment { get; set; } = "production";

    public string CollectorEndpoint { get; set; } = "http://localhost:4317";

    public bool EnableConsoleExporter { get; set; } = false;

    public TracingSettings Tracing { get; set; } = new();

    public MetricsSettings Metrics { get; set; } = new();

    public LoggingSettings Logging { get; set; } = new();

    public Dictionary<string, object> ResourceAttributes { get; set; } = [];
}

public class OpenTelemetryOptionsValidator : IValidateOptions<OpenTelemetrySettings>
{
    public ValidateOptionsResult Validate(string? name, OpenTelemetrySettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            failures.Add("ServiceName is required");
        }

        if (string.IsNullOrWhiteSpace(options.CollectorEndpoint))
        {
            failures.Add("CollectorEndpoint is required");
        }
        else if (!Uri.TryCreate(options.CollectorEndpoint, UriKind.Absolute, out _))
        {
            failures.Add("CollectorEndpoint must be a valid URI");
        }

        if (options.Tracing.SamplingRatio < 0 || options.Tracing.SamplingRatio > 1)
        {
            failures.Add("SamplingRatio must be between 0 and 1");
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}