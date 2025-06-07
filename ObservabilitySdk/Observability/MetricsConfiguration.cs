namespace ObservabilitySdk.Observability;

public class MetricsConfiguration
{
    public bool Enabled { get; set; } = true;
    public string PrometheusEndpoint { get; set; } = "/metrics";
    public int ScrapeIntervalSeconds { get; set; } = 15;
    public List<string> CustomMetrics { get; set; } = [];
}