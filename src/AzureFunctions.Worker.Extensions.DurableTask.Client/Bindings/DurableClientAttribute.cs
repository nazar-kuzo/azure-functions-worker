using System.Collections.Concurrent;
using System.Reflection;
using AzureFunctions.Worker.Extensions.DurableTask.Client.Internal;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Extensions.DurableTask;

/// <summary>
/// Attribute used to bind a function parameter to a <see cref="DurableTaskClient"/> instance.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class DurableClientAttribute() : InputConverterAttribute(typeof(DurableClientConverter))
{
    /// <summary>
    /// Specifies target Durable TaskHub name if multiple hubs are used within application.
    /// </summary>
    public string? Name { get; set; }
}

internal sealed class DurableClientConverter : IInputConverter
{
    private static readonly ConcurrentDictionary<string, string?> DurableClientNameCache = new();

    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        var durableClientName = DurableClientNameCache.GetOrAdd(context.FunctionContext.FunctionId, _ =>
        {
            var functionMethodInfo = context.FunctionContext.InstanceServices
                .GetRequiredService<FunctionMethodInfoLocator>()
                .GetMethodInfo(context.FunctionContext);

            return functionMethodInfo.GetParameters()
                .First(parameter => parameter.ParameterType == context.TargetType)
                .GetCustomAttribute<DurableClientAttribute>()
                !.Name;
        });

        var client = context.FunctionContext.InstanceServices
            .GetRequiredKeyedService<DurableTaskClient>(durableClientName);

        return ValueTask.FromResult(ConversionResult.Success(client));
    }
}