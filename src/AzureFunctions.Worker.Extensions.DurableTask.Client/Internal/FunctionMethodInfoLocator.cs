using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFunctions.Worker.Extensions.DurableTask.Client.Internal;

/// <summary>
/// Helps to locate <see cref="MethodInfo"/> for specified function ID.
/// This helper is often used during function parameter binding process.
/// </summary>
internal sealed class FunctionMethodInfoLocator
{
    private readonly ConcurrentDictionary<string, MethodInfo> functions = new();
    private readonly Func<string, string, MethodInfo> methodInfoProvider;

    public FunctionMethodInfoLocator(IServiceProvider serviceProvider)
    {
        var methodInfoLocatorType = typeof(FunctionContext).Assembly
            .GetType("Microsoft.Azure.Functions.Worker.Invocation.IMethodInfoLocator")!;

        var methodInfoLocator = serviceProvider.GetRequiredService(methodInfoLocatorType);

        this.methodInfoProvider = methodInfoLocatorType
            .GetMethod("GetMethod")!
            .CreateDelegate<Func<string, string, MethodInfo>>(methodInfoLocator);
    }

    public MethodInfo GetMethodInfo(FunctionContext functionContext)
    {
        var functionDefinition = functionContext.FunctionDefinition;

        return this.functions.GetOrAdd(
            functionDefinition.Id,
            (key, definition) => this.methodInfoProvider(definition.PathToAssembly, definition.EntryPoint),
            functionDefinition);
    }
}
