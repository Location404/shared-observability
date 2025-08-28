using System.Diagnostics.Metrics;

namespace Shared.Observability.Core;


public class ObservabilityMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _errorCounter;

    public ObservabilityMetrics(string serviceName)
    {
        _meter = new Meter(serviceName);

        _requestCounter = _meter.CreateCounter<long>(
            "http_requests_total",
            "total",
            "Total number of HTTP requests");

        _requestDuration = _meter.CreateHistogram<double>(
            "http_request_duration_seconds",
            "seconds",
            "Duration of HTTP requests");

        _errorCounter = _meter.CreateCounter<long>(
            "errors_total",
            "total",
            "Total number of errors");
    }

    public void IncrementRequests(string method, string endpoint, int statusCode)
    {
        _requestCounter.Add(delta: 1,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status_code", statusCode));
    }

    public void RecordRequestDuration(double duration, string method, string endpoint)
    {
        _requestDuration.Record(value: duration,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("endpoint", endpoint));
    }

    public void IncrementErrors(string errorType, string operation)
    {
        _errorCounter.Add(delta: 1,
            new KeyValuePair<string, object?>("error_type", errorType),
            new KeyValuePair<string, object?>("operation", operation));
    }

    public void Dispose()
    {
        _meter?.Dispose();
        GC.SuppressFinalize(this);
    }
}