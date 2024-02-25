using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

internal sealed class StandaloneFunctionsTelemetryModule(
    TelemetryClientAccessor activityContext)
    : ITelemetryModule, IAsyncDisposable
{
    private ActivityListener? hostActivityListener;

    public void Initialize(TelemetryConfiguration configuration)
    {
        activityContext.TelemetryClient = new TelemetryClient(configuration);

        // TODO: investigate possibility to drop this listener
        this.hostActivityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Microsoft.Azure.Functions.Worker"),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(this.hostActivityListener);
    }

    public async ValueTask DisposeAsync()
    {
        this.hostActivityListener?.Dispose();

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
