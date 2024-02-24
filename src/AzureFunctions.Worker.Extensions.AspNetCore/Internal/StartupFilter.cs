using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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
                ReplaceFunctionEndpointDataSource(builder, endpointRouteBuilder);
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

    private static void ReplaceFunctionEndpointDataSource(
        IApplicationBuilder builder,
        IEndpointRouteBuilder endpointRouteBuilder)
    {
        var existingDataSource = endpointRouteBuilder.DataSources.First();

        endpointRouteBuilder.DataSources.Remove(existingDataSource);

        var newDataSource = ActivatorUtilities.CreateInstance<AspNetCoreFunctionEndpointDataSource>(
            builder.ApplicationServices,
            existingDataSource);

        endpointRouteBuilder.DataSources.Add(newDataSource);
    }
}
