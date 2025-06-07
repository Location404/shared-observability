namespace ObservabilitySdk.Observability;

public class TracingConfiguration
{
    public bool Enabled { get; set; } = true;
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public double SamplingRatio { get; set; } = 1.0;
    public List<string> IgnoredPaths { get; set; } = ["/health", "/metrics"];
}