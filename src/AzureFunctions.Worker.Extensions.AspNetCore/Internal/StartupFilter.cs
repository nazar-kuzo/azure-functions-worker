using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal;

/// <summary>
/// Startup filter that modifies existing Worker.Extensions.Http.AspNetCore extension.
/// <list type="number">
/// <item><description>
/// Replaces existing <see cref="EndpointDataSource"/> with <see cref="AspNetCoreFunctionEndpointDataSource"/>
/// </description></item>
/// </list>
/// </summary>
internal class StartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            next(builder);

            if (TryGetEndpointRouteBuilder(builder) is { } endpointRouteBuilder)
            {
                endpointRouteBuilder.MapControllers();

                ReplaceExistingDataSources(endpointRouteBuilder);
            }
        };
    }

    private static IEndpointRouteBuilder? TryGetEndpointRouteBuilder(IApplicationBuilder application)
    {
        if (application.Properties.TryGetValue("__EndpointRouteBuilder", out var result))
        {
            return (IEndpointRouteBuilder) result!;
        }

        return null;
    }

    private static void ReplaceExistingDataSources(IEndpointRouteBuilder endpointRouteBuilder)
    {
        var functionEndpointDataSource = endpointRouteBuilder.DataSources.First();
        var controllerEndpointDataSource = endpointRouteBuilder.DataSources.Skip(1).First();

        endpointRouteBuilder.DataSources.Clear();

        endpointRouteBuilder.DataSources.Add(new AspNetCoreFunctionEndpointDataSource(
            functionEndpointDataSource,
            controllerEndpointDataSource));
    }
}
