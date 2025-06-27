using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal.Middlewares;

/// <summary>
/// Middleware that responds to invalid <see cref="ActionContext.ModelState"/>.
/// This middleware mimics <see cref="ModelStateInvalidFilter"/> in aspnet core.
/// </summary>
/// <param name="parameterBinder">AspNet core function parameter binder</param>
/// <param name="functionMetadataProvider">Function metadata provider</param>
/// <param name="mvcOptions">Mvc options</param>
/// <param name="actionContextAccessor">Action context accessor</param>
internal class AspNetCoreModelStateValidationMiddleware(
    IActionContextAccessor actionContextAccessor,
    ParameterBinder parameterBinder,
    AspNetCoreFunctionMetadataProvider functionMetadataProvider,
    IOptions<MvcOptions> mvcOptions) : IMiddleware
{
    private static readonly Type BindingCacheType = typeof(FunctionContext).Assembly
        .GetType("Microsoft.Azure.Functions.Worker.IBindingCache`1")!
        .MakeGenericType(typeof(ConversionResult));

    private static readonly ControllerBase ModelStateController = new FakeController();
    private static readonly MethodInfo TryAddToBindingCache = BindingCacheType.GetMethod("TryAdd")!;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var functionContext = context.GetFunctionContext()!;

        var metadata = functionMetadataProvider.GetFunctionMetadata(functionContext.FunctionDefinition.Name);

        if (metadata.AspNetCoreParameters.Length == 0)
        {
            await next(context);
        }

        var actionContext = actionContextAccessor.ActionContext!;
        var cacheBindingInput = CreateBindingInputCacheSetter(functionContext);
        var actionParameters = new Dictionary<string, object?>(metadata.AspNetCoreParameters.Length);

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

                actionParameters.Add(parameterInfo.Parameter.Name, result.Model);

                if (result.IsModelSet)
                {
                    cacheBindingInput(parameterInfo.Parameter.Name, ConversionResult.Success(result.Model));
                }
            }
            catch (ValueProviderException ex)
            {
                actionParameters.Add(parameterInfo.Parameter.Name, null);

                actionContext.ModelState.AddModelError(
                    parameterInfo.ModelMetadata.Name ?? parameterInfo.Parameter.Name,
                    ex.Message);
            }
        }

        var modelStateFilter = context.RequestServices.GetRequiredService<IFilterMetadata>();

        var actionExecutingContext = new ActionExecutingContext(actionContext, [modelStateFilter], actionParameters, ModelStateController);

        if (modelStateFilter is IActionFilter filter)
        {
            filter.OnActionExecuting(actionExecutingContext);
        }
        else if (modelStateFilter is IAsyncActionFilter asyncFilter)
        {
            await asyncFilter.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult<ActionExecutedContext>(null!));
        }

        if (actionExecutingContext.Result is { } actionResult)
        {
            await actionResult.ExecuteResultAsync(actionContext);
        }
        else
        {
            await next(context);
        }
    }

    private static Func<string, ConversionResult, bool> CreateBindingInputCacheSetter(FunctionContext functionContext)
    {
        var bindingCache = functionContext.InstanceServices.GetRequiredService(BindingCacheType);

        return TryAddToBindingCache.CreateDelegate<Func<string, ConversionResult, bool>>(bindingCache);
    }
}

/// <summary>
/// Fake controller that should be passed to
/// <see cref="ActionExecutingContext"/> in order to do parameters validation
/// </summary>
file sealed class FakeController : ControllerBase
{
}
