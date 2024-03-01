using Microsoft.AspNetCore.Mvc.Abstractions;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal;

/// <summary>
/// Provides Function action descriptors into AspNetCore MVC pipeline
/// </summary>
/// <param name="functionMetadataProvider">AspNetCore function metadata provider</param>
internal class FunctionActionDescriptorProvider(
    AspNetCoreFunctionMetadataProvider functionMetadataProvider)
    : IActionDescriptorProvider
{
    public int Order => 0;

    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
        foreach (var metadata in functionMetadataProvider.Metadata.Values)
        {
            context.Results.Add(metadata.ActionDescriptor);
        }
    }

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        // no futher actions needed
    }
}