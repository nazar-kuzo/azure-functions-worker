using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker;

public static class FunctionContextExtensions
{
    public static ILogger GetFunctionLogger(this FunctionContext functionContext)
    {
        return functionContext.InstanceServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger($"Functions.{functionContext.FunctionDefinition.Name}");
    }
}
