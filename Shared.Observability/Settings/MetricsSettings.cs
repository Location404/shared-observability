namespace Shared.Observability.Settings;

public class MetricsSettings
{
    public bool Enabled { get; set; } = true;
    public List<string> CustomMeters { get; set; } = [];
}