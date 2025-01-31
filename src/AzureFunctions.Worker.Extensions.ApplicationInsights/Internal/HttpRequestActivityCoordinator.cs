using System.Collections.Concurrent;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

/// <summary>
/// Helps to exchange <see cref="IOperationHolder{T}"/> between HTTP triggered function
/// in order to set additional HTTP related information
/// </summary>
internal class HttpRequestActivityCoordinator
{
    private readonly ConcurrentDictionary<string, RequestActivity> inflightRequestActivities = new();

    /// <summary>
    /// Stores <see cref="RequestTelemetry"/> for the given invocationId, to be picked up later by <see cref="WorkerHttpRequestMappingMiddleware"/>
    /// </summary>
    /// <param name="invocationId">Function invocation ID</param>
    /// <param name="requestTelemetry">Application Insights request telemetry</param>
    /// <param name="cancellationToken">Request cancellation token</param>
    public void StartRequestActivity(
        string invocationId,
        RequestTelemetry requestTelemetry,
        CancellationToken cancellationToken)
    {
        this.inflightRequestActivities.AddOrUpdate(
            invocationId,
            (invocationId, telemetry) =>
            {
                var requestActivity = new RequestActivity();

                requestActivity.SetCancellationToken(cancellationToken);

                requestActivity.Telemetry.SetResult(telemetry);

                return requestActivity;
            },
            (invocationId, requestActivity, telemetry) =>
            {
                requestActivity.SetCancellationToken(cancellationToken);

                requestActivity.Telemetry.SetResult(telemetry);

                return requestActivity;
            },
            requestTelemetry);
    }

    /// <summary>
    /// Removes <see cref="RequestTelemetry"/> for the given invocationId
    /// </summary>
    /// <param name="invocationId">Function invocation ID</param>
    public void CompleteRequestActivity(string invocationId)
    {
        if (this.inflightRequestActivities.TryRemove(invocationId, out var requestActivity))
        {
            requestActivity.Stop();
        }
    }

    /// <summary>
    /// <see cref="WorkerHttpRequestMappingMiddleware"/> execution is blocked until
    /// <see cref="FunctionRequestTelemetryMiddleware"/> stores <see cref="RequestActivity"/>
    /// </summary>
    /// <param name="invocationId">Function invocation ID</param>
    /// <returns>Application Insights request telemetry</returns>
    public Task<RequestTelemetry> WaitForRequestActivityStartedAsync(string invocationId)
    {
        var requestActivity = this.inflightRequestActivities.GetOrAdd(
            invocationId,
            invocationId => new RequestActivity());

        return requestActivity.Telemetry.Task;
    }

    /// <summary>
    /// <see cref="FunctionRequestTelemetryMiddleware"/> should wait until
    /// <see cref="WorkerHttpRequestMappingMiddleware"/> execution is completed in order to dispose request telemetry
    /// </summary>
    /// <param name="invocationId">Function invocation ID</param>
    /// <returns>Task</returns>
    public async Task WaitForRequestActivityCompletedAsync(string invocationId)
    {
        if (this.inflightRequestActivities.TryGetValue(invocationId, out var requestActivity))
        {
            await requestActivity.Completed.Task;
        }
    }
}

internal class RequestActivity : IDisposable
{
    private CancellationToken token;
    private CancellationTokenRegistration tokenRegistration;
    private bool disposedValue;

    public TaskCompletionSource<RequestTelemetry> Telemetry { get; set; } = new();

    public TaskCompletionSource<bool> Completed { get; set; } = new();

    public void SetCancellationToken(CancellationToken token)
    {
        this.token = token;
        this.tokenRegistration = token.Register(() =>
        {
            this.Completed.TrySetCanceled();
        });
    }

    public void Stop()
    {
        if (this.Telemetry.Task.IsCompleted)
        {
            if (this.Telemetry.Task.IsCanceled || this.token.IsCancellationRequested)
            {
                this.Completed.TrySetCanceled();
            }
            else
            {
                this.Completed.SetResult(true);
            }
        }
    }

    public void Dispose()
    {
        this.Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing && this.tokenRegistration != default)
            {
                this.tokenRegistration.Dispose();
            }

            this.disposedValue = true;
        }
    }
}