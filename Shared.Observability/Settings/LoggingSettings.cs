namespace Shared.Observability.Settings;

public class LoggingSettings
{
    public bool Enabled { get; set; } = true;

    public bool IncludeFormattedMessage { get; set; } = true;

    public bool IncludeScopes { get; set; } = true;

    public bool ParseStateValues { get; set; } = true;
}