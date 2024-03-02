using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal;

/// <summary>
/// Decorates existing endpoints provided by default <see cref="EndpointDataSource"/>
/// with AspNetCore metadata like <see cref="IAuthorizeData"/> etc.
/// </summary>
/// <param name="functionEndpointDataSource">Worker default endpoint data source</param>
/// <param name="controllerEndpointDataSource">AspNetCore Controller endpoint data source</param>
internal class AspNetCoreFunctionEndpointDataSource(
    EndpointDataSource functionEndpointDataSource,
    EndpointDataSource controllerEndpointDataSource
    ) : EndpointDataSource
{
    private readonly object @lock = new();

    private IReadOnlyList<Endpoint>? endpoints;

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            if (this.endpoints is null)
            {
                lock (this.@lock)
                {
                    this.endpoints ??= this.CreateEndpoints();
                }
            }

            return this.endpoints;
        }
    }

    public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

    private ReadOnlyCollection<Endpoint> CreateEndpoints()
    {
        var controllerEndpointMetadata = controllerEndpointDataSource.Endpoints
            .ToDictionary(endpoint => endpoint.DisplayName!, endpoint => endpoint.Metadata);

        return functionEndpointDataSource.Endpoints
            .Cast<RouteEndpoint>()
            .Select(endpoint =>
            {
                var builder = new RouteEndpointBuilder(endpoint.RequestDelegate, endpoint.RoutePattern, endpoint.Order)
                {
                    DisplayName = endpoint.DisplayName,
                };

                controllerEndpointMetadata.TryGetValue(endpoint.DisplayName!, out EndpointMetadataCollection? metadata);

                foreach (var item in endpoint.Metadata.Union(metadata?.AsEnumerable() ?? Enumerable.Empty<object>()))
                {
                    builder.Metadata.Add(item);
                }

                return builder.Build();
            })
            .ToList()
            .AsReadOnly();
    }
}
