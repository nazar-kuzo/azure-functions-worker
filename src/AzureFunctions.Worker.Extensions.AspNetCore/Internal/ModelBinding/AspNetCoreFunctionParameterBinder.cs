using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal.ModelBinding;

internal class AspNetCoreFunctionParameterBinder(
    ParameterBinder parameterBinder,
    AspNetCoreFunctionMetadataProvider functionMetadataProvider,
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

        var httpContext = functionContext.GetHttpContext()!;
        var cacheBindingInput = CreateBindingInputCacheSetter(functionContext);

        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            ActionDescriptor = metadata.ActionDescriptor,
            RouteData = httpContext.GetRouteData(),
        };

        foreach (var parameterInfo in metadata.AspNetCoreParameters)
        {
            try
            {
                var result = await parameterBinder.BindModelAsync(
                        actionContext,
                        parameterInfo.ModelBinder,
                        await CompositeValueProvider.CreateAsync(actionContext, mvcOptions.Value.ValueProviderFactories),
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
                actionContext.ModelState.AddModelError(
                    parameterInfo.ModelMetadata.Name ?? parameterInfo.Parameter.Name,
                    ex.Message);
            }
        }

        if (!actionContext.ModelState.IsValid)
        {
            var validationResult = (ObjectResult) apiBehaviorOptions.Value.InvalidModelStateResponseFactory(actionContext);

            if (validationResult.Value is ValidationProblemDetails problemDetails)
            {
                problemDetails.Extensions["traceId"] = functionContext.InvocationId;
            }

            await validationResult.ExecuteResultAsync(actionContext);

            throw new InvalidOperationException("One or more validation errors occurred.");
        }
    }

    private static Func<string, ConversionResult, bool> CreateBindingInputCacheSetter(FunctionContext functionContext)
    {
        var bindingCache = functionContext.InstanceServices.GetRequiredService(BindingCacheType);

        return TryAddToBindingCache.CreateDelegate<Func<string, ConversionResult, bool>>(bindingCache);
    }
}
