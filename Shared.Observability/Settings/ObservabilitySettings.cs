namespace Shared.Observability.Settings;

public class ObservabilitySettings
{
    public const string SectionName = "Observability";
    public string ApplicationName { get; set; } = "DefaultApplication";
    public LokiSettings Loki { get; set; } = new();
    public PrometheusSettings Prometheus { get; set; } = new();
    public TracingSettings Tracing { get; set; } = new();
    public HealthCheckSettings HealthChecks { get; set; } = new();
    public MetricsSettings Metrics { get; set; } = new();
}

public class LokiSettings
{
    public bool Enabled { get; set; }
    public string EndpointUrl { get; set; } = "http://localhost:4317";
}

public class PrometheusSettings
{
    public bool Enabled { get; set; }
}

public class TracingSettings
{
    public bool Enabled { get; set; }
    public string EndpointUrl { get; set; } = "http://localhost:4317";
}

public class HealthCheckSettings
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "/health";
}

public class MetricsSettings
{
    public string EndpointUrl { get; set; } = "http://localhost:4317";
}