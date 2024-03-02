using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal.ModelBinding;

internal class AspNetCoreFunctionInputBindingFeature(
    AspNetCoreFunctionParameterBinder functionParameterBinder,
    IFunctionInputBindingFeature defaultInputBindingFeature)
    : IFunctionInputBindingFeature
{
    public async ValueTask<FunctionInputBindingResult> BindFunctionInputAsync(FunctionContext context)
    {
        await functionParameterBinder.BindAspNetCoreFunctionInputAsync(context);

        return await defaultInputBindingFeature.BindFunctionInputAsync(context);
    }
}