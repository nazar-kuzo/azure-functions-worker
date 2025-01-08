using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal;

/// <summary>
/// Patches generated API descriptions to fix return
/// type of HTTP triggered functions with multiple outputs
/// </summary>
internal class FunctionApiDescriptionProvider(
    IModelMetadataProvider modelMetadataProvider,
    AspNetCoreFunctionMetadataProvider functionMetadataProvider)
    : IApiDescriptionProvider
{
    public int Order => 0;

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        foreach (var apiDescription in context.Results)
        {
            var metadata = functionMetadataProvider
                .GetFunctionMetadata(apiDescription.ActionDescriptor.DisplayName!);

            if (metadata?.HttpResultDataType != null)
            {
                foreach (var responseType in apiDescription.SupportedResponseTypes)
                {
                    if (responseType.Type != null && responseType.Type != metadata.HttpResultDataType)
                    {
                        responseType.Type = metadata.HttpResultDataType;
                        responseType.ModelMetadata = modelMetadataProvider.GetMetadataForType(metadata.HttpResultDataType);
                    }
                }
            }
        }
    }

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        // this provider wont create api descriptions
    }
}
