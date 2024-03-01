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
/// <param name="metadataProvider">AspNetCore Function metadata provider</param>
internal class AspNetCoreFunctionEndpointDataSource(
    EndpointDataSource functionEndpointDataSource,
    EndpointDataSource controllerEndpointDataSource,
    AspNetCoreFunctionMetadataProvider metadataProvider
    ) : EndpointDataSource
{
    private readonly object @lock = new();

    private List<Endpoint>? endpoints;

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            if (this.endpoints is null)
            {
                lock (this.@lock)
                {
                    var controllerEndpointMetadata = controllerEndpointDataSource.Endpoints
                        .ToDictionary(endpoint => endpoint.DisplayName!, endpoint => endpoint.Metadata);

                    this.endpoints ??= functionEndpointDataSource.Endpoints
                        .Cast<RouteEndpoint>()
                        .Select(endpoint =>
                        {
                            var builder = new RouteEndpointBuilder(endpoint.RequestDelegate, endpoint.RoutePattern, endpoint.Order)
                            {
                                DisplayName = endpoint.DisplayName,
                            };

                            var metadata = metadataProvider.GetFunctionMetadata(endpoint.DisplayName!);

                            var aspnetCoreMetadata = controllerEndpointMetadata.ContainsKey(metadata.Name!)
                                ? controllerEndpointMetadata[metadata.Name!].AsEnumerable()
                                : Enumerable.Empty<object>();

                            foreach (var item in endpoint.Metadata.Union(aspnetCoreMetadata))
                            {
                                builder.Metadata.Add(item);
                            }

                            return builder.Build();
                        })
                        .ToList();
                }
            }

            return this.endpoints;
        }
    }

    public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;
}
