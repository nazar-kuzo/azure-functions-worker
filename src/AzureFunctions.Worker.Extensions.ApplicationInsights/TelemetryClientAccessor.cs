using Microsoft.ApplicationInsights;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights;

public class TelemetryClientAccessor
{
    public TelemetryClient? TelemetryClient { get; internal set; }
}
