using System.Diagnostics;
using OpenTelemetry;

namespace Shared.Observability.Core;

public static class ActivitySourceExtensions
{
    public static Activity? StartActivityWithTags( this ActivitySource source,
        string name,
        Dictionary<string, object?> tags,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = source.StartActivity(name, kind);

        if (activity != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    public static Activity? SetBaggage(this Activity? activity, string key, string value)
    {
        if (activity != null)
        {
            Baggage.SetBaggage(key, value);
        }
        return activity;
    }
}