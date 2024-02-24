using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

/// <summary>
/// Filters out internal function context properties and
/// maps any <see cref="Activity.Baggage"/> properties to request
/// </summary>
/// <param name="next">Next telemetry processor</param>
internal class FunctionTelemetryProcessor(ITelemetryProcessor next) : ITelemetryProcessor
{
    public void Process(ITelemetry item)
    {
        if (item is DependencyTelemetry dependency &&
            dependency.Name.EndsWith("AzureFunctionsRpcMessages.FunctionRpc/EventStream"))
        {
            return;
        }

        if (item is ISupportProperties telemetry)
        {
            // ignore properties propagated from SDK
            telemetry.Properties.Remove("az.schema_url");
            telemetry.Properties.Remove("faas.execution");
            telemetry.Properties.Remove("AzureFunctions_FunctionName");
            telemetry.Properties.Remove("AzureFunctions_InvocationId");
        }

        next.Process(item);
    }
}