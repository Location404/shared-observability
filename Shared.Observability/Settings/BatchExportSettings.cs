
namespace Shared.Observability.Settings;

public class BatchExportSettings
{
    public int MaxQueueSize { get; set; } = 2048;

    public int ScheduledDelayMilliseconds { get; set; } = 5000;

    public int ExporterTimeoutMilliseconds { get; set; } = 30000;

    public int MaxExportBatchSize { get; set; } = 512;
}