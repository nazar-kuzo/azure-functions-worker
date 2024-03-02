using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal;

/// <summary>
/// Provides Function action descriptors into AspNetCore MVC pipeline
/// </summary>
/// <param name="modelBinderFactory">Model Binder Factory</param>
/// <param name="modelMetadataProvider">Model Metadata Provider</param>
/// <param name="functionMetadataProvider">AspNetCore function metadata provider</param>
internal class FunctionActionDescriptorProvider(
    IModelBinderFactory modelBinderFactory,
    IModelMetadataProvider modelMetadataProvider,
    AspNetCoreFunctionMetadataProvider functionMetadataProvider)
    : IActionDescriptorProvider
{
    public int Order => 0;

    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
        // no further actions needed
    }

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        _ = context.Results
            .Cast<ControllerActionDescriptor>()
            .Join(functionMetadataProvider.Metadata.Values, action => action.MethodInfo, metadata => metadata.TargetMethod, (action, metadata) =>
            {
                metadata.ActionDescriptor = action;
                action.DisplayName = metadata.Name!;

                RemoveNonAspNetCoreParameters(action);

                metadata.AspNetCoreParameters = action.Parameters
                    .Cast<ControllerParameterDescriptor>()
                    .Select(parameter =>
                    {
                        var modelMetadata = ((ModelMetadataProvider) modelMetadataProvider)
                            .GetMetadataForParameter(parameter.ParameterInfo);

                        var binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
                        {
                            BindingInfo = parameter.BindingInfo,
                            Metadata = modelMetadata,
                            CacheToken = parameter,
                        });

                        return new AspNetCoreParameterBindingInfo(binder, modelMetadata, parameter);
                    })
                    .Where(item => item.ModelMetadata.IsBindingAllowed)
                    .ToArray();

                return action;
            })
            .ToArray();
    }

    private static void RemoveNonAspNetCoreParameters(ControllerActionDescriptor action)
    {
        var nonAspNetCoreParameters = action.Parameters
            .Where(parameter =>
                parameter.BindingInfo == null ||
                parameter.BindingInfo.BindingSource == BindingSource.Special)
            .ToList();

        foreach (var parameter in nonAspNetCoreParameters)
        {
            action.Parameters.Remove(parameter);
        }
    }
}