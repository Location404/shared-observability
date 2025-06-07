namespace ObservabilitySdk.Observability;

public class LoggingConfiguration
{
    public bool Enabled { get; set; } = true;
    public string MinimumLevel { get; set; } = "Information";
    public bool StructuredLogging { get; set; } = true;
    public OtlpLogExporter OtlpExporter { get; set; } = new();
}