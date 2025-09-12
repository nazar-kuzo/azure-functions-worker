using AzureFunctions.Worker.Extensions.DurableTask.Client.Internal;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class WorkerExtensions
{
    /// <summary>
    /// Configures the DurableTask clients for the Azure Functions Worker.
    /// </summary>
    /// <param name="worker">Functions application builder</param>
    /// <param name="sectionName">Configuration section name</param>
    /// <returns>FunctionsApplicationBuilder</returns>
    public static FunctionsApplicationBuilder ConfigureDurableTaskClient(
        this FunctionsApplicationBuilder worker,
        string sectionName = "DurableTask")
    {
        worker.ConfigureDurableTaskClient(sectionName, clientOptions =>
        {
            clientOptions.ConnectionString ??= worker.Configuration.GetValue<string>("AzureWebJobsStorage")!;
            clientOptions.TaskHubName ??= "TestHubName";
        });

        worker.Services.AddSingleton<FunctionMethodInfoLocator>();

        return worker;
    }
}
