namespace Shared.Observability.Settings;

public class TracingSettings
{
    public bool Enabled { get; set; } = true;
    
    public double SamplingRatio { get; set; } = 1.0;
    
    public bool RecordExceptions { get; set; } = true;
    
    public List<string> IgnorePaths { get; set; } = ["/health", "/metrics", "/ready", "/live"];
    
    public List<string> IgnoreHosts { get; set; } = ["localhost"];
    
    public BatchExportSettings BatchExport { get; set; } = new();
    
    public bool EnableGrpcSupport { get; set; } = false;
}