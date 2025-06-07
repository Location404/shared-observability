namespace ObservabilitySdk.Observability;

public class ObservabilityConfiguration
{
    public string ServiceName { get; set; } = "unknown-service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "development";
    
    public MetricsConfiguration Metrics { get; set; } = new();
    public TracingConfiguration Tracing { get; set; } = new();
    public LoggingConfiguration Logging { get; set; } = new();
    public HealthCheckConfiguration HealthChecks { get; set; } = new();
}