using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

internal sealed class StandaloneFunctionsTelemetryModule(
    TelemetryClientAccessor activityContext)
    : ITelemetryModule, IAsyncDisposable
{
    public void Initialize(TelemetryConfiguration configuration)
    {
        activityContext.TelemetryClient = new TelemetryClient(configuration);
    }

    public async ValueTask DisposeAsync()
    {
        if (activityContext.TelemetryClient != null)
        {
            using var cts = new CancellationTokenSource(millisecondsDelay: 5000);

            try
            {
                await activityContext.TelemetryClient.FlushAsync(cts.Token);
            }
            catch
            {
                // do nothing since telemetry failed to flush during app shutdown
            }
        }

        GC.SuppressFinalize(this);
    }
}
