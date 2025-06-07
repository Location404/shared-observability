namespace ObservabilitySdk.Observability;

public class OtlpLogExporter
{
    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = "http://localhost:4318";
}