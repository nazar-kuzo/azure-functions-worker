using AzureFunctions.Worker.Extensions.ApplicationInsights;
using AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.Hosting;

[assembly: WorkerExtensionStartup(typeof(ApplicationInsightsExtensionStartup))]

namespace AzureFunctions.Worker.Extensions.ApplicationInsights;

/// <summary>
/// Registers ApplicationInsights middleware that converts In-Proc dependency calls into request
/// </summary>
// TODO: investigate ability to register middleware in worker extension instead of using hook
public class ApplicationInsightsExtensionStartup : WorkerExtensionStartup
{
    public override void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<FunctionApplicationInsightsMiddleware>();
    }
}
