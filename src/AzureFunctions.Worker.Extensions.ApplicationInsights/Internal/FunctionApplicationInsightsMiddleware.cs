using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

/// <summary>
/// Starts <see cref="RequestTelemetry"/> operation based on worker host activity.
/// Request telemetry could be hijacked by HTTP middleware if function is triggered by HTTP
/// </summary>
/// <param name="telemetryClient">Telemetry client</param>
/// <param name="activityCoordinator">Function activity coordinator</param>
internal class FunctionApplicationInsightsMiddleware(
    TelemetryClient telemetryClient,
    FunctionActivityCoordinator activityCoordinator)
    : IFunctionsWorkerMiddleware
{
    private const string HttpTrigger = "httpTrigger";

    private readonly ConcurrentDictionary<string, bool> httpTriggerFunctions = new();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var hostActivity = Activity.Current!;

        using var requestActivity = telemetryClient.StartOperation<RequestTelemetry>(hostActivity);

        requestActivity.Telemetry.Name = context.FunctionDefinition.Name;
        requestActivity.Telemetry.Context.Operation.Name = context.FunctionDefinition.Name;

        activityCoordinator.StartRequestActivity(context.InvocationId, requestActivity.Telemetry, context.CancellationToken);

        var success = default(bool?);

        try
        {
            await next(context);
        }
        catch
        {
            success = false;

            throw;
        }
        finally
        {
            if (success == null && this.IsHttpTriggerFunction(context))
            {
                try
                {
                    await activityCoordinator.WaitForRequestActivityCompletedAsync(context.InvocationId);
                }
                catch (TaskCanceledException)
                {
                    requestActivity.Telemetry.ResponseCode = "(cancelled)";
                    requestActivity.Telemetry.Success = false;
                }
            }

            requestActivity.Telemetry.Success ??= success ?? true;

            foreach (var property in hostActivity.Baggage)
            {
                requestActivity.Telemetry.Properties.TryAdd(property.Key, property.Value);
            }
        }
    }

    private bool IsHttpTriggerFunction(FunctionContext context)
    {
        return this.httpTriggerFunctions.GetOrAdd(
            context.FunctionId,
            static (_, context) => context.FunctionDefinition.InputBindings
                .Any(binding => binding.Value.Type.Equals(HttpTrigger, StringComparison.OrdinalIgnoreCase)),
            context);
    }
}
