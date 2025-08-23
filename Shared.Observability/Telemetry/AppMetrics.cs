using System.Diagnostics.Metrics;
using Shared.Observability.Settings;

namespace Shared.Observability.Telemetry;

public class AppMetrics
{
    public Counter<int> OrdersProcessed { get; }
    public Histogram<double> OrderProcessingDuration { get; }

    public AppMetrics(IMeterFactory meterFactory, ObservabilitySettings settings)
    {
        var meter = meterFactory.Create(settings.ApplicationName);

        OrdersProcessed = meter.CreateCounter<int>(
            name: "orders_processed_total", 
            description: "Total number of orders processed.");

        OrderProcessingDuration = meter.CreateHistogram<double>(
            name: "order_processing_duration_seconds", 
            unit: "s", 
            description: "Histogram of order processing duration.");
    }
}