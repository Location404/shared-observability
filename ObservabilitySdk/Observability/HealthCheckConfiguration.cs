namespace ObservabilitySdk.Observability;

public class HealthCheckConfiguration
{
    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = "/health";
    public int TimeoutSeconds { get; set; } = 5;
}