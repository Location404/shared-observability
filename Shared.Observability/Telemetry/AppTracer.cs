using System.Diagnostics;
using Shared.Observability.Settings;

namespace Shared.Observability.Telemetry;

public class AppTracer(ObservabilitySettings settings)
{
    public ActivitySource Source { get; } = new ActivitySource(settings.ApplicationName);
}