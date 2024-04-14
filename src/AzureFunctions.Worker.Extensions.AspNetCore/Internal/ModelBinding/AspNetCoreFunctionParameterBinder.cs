using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal.ModelBinding;

internal class AspNetCoreFunctionParameterBinder(
    ParameterBinder parameterBinder,
    AspNetCoreFunctionMetadataProvider functionMetadataProvider,
    IActionContextAccessor actionContextAccessor,
    IOptions<MvcOptions> mvcOptions,
    IOptions<ApiBehaviorOptions> apiBehaviorOptions)
{
    private static readonly Type BindingCacheType = typeof(FunctionContext).Assembly
        .GetType("Microsoft.Azure.Functions.Worker.IBindingCache`1")!
        .MakeGenericType(typeof(ConversionResult));

    private static readonly MethodInfo TryAddToBindingCache = BindingCacheType.GetMethod("TryAdd")!;

    public async Task BindAspNetCoreFunctionInputAsync(FunctionContext functionContext)
    {
        var metadata = functionMetadataProvider.GetFunctionMetadata(functionContext.FunctionDefinition.Name);

        if (metadata.AspNetCoreParameters.Length == 0)
        {
            return;
        }

        var cacheBindingInput = CreateBindingInputCacheSetter(functionContext);

        foreach (var parameterInfo in metadata.AspNetCoreParameters)
        {
            try
            {
                var result = await parameterBinder.BindModelAsync(
                        actionContextAccessor.ActionContext!,
                        parameterInfo.ModelBinder,
                        await CompositeValueProvider.CreateAsync(actionContextAccessor.ActionContext!, mvcOptions.Value.ValueProviderFactories),
                        parameterInfo.Parameter,
                        parameterInfo.ModelMetadata,
                        value: null,
                        container: null);

                if (result.IsModelSet)
                {
                    cacheBindingInput(parameterInfo.Parameter.Name, ConversionResult.Success(result.Model));
                }
            }
            catch (ValueProviderException ex)
            {
                actionContextAccessor.ActionContext!.ModelState.AddModelError(
                    parameterInfo.ModelMetadata.Name ?? parameterInfo.Parameter.Name,
                    ex.Message);
            }
        }

        if (!actionContextAccessor.ActionContext!.ModelState.IsValid)
        {
            var validationResult = (ObjectResult) apiBehaviorOptions.Value.InvalidModelStateResponseFactory(actionContextAccessor.ActionContext);

            if (validationResult.Value is ValidationProblemDetails problemDetails)
            {
                problemDetails.Extensions["traceId"] = functionContext.InvocationId;
            }

            await validationResult.ExecuteResultAsync(actionContextAccessor.ActionContext);

            throw new InvalidOperationException("One or more validation errors occurred.");
        }
    }

    private static Func<string, ConversionResult, bool> CreateBindingInputCacheSetter(FunctionContext functionContext)
    {
        var bindingCache = functionContext.InstanceServices.GetRequiredService(BindingCacheType);

        return TryAddToBindingCache.CreateDelegate<Func<string, ConversionResult, bool>>(bindingCache);
    }
}
