using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

/// <summary>
/// Registers application middleware responsible to map
/// HTTP triggered function properties to <see cref="RequestTelemetry"/>
/// </summary>
internal class WorkerStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<WorkerHttpRequestMappingMiddleware>();

            next(builder);
        };
    }
}
