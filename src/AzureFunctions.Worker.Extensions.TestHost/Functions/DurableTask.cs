using DurableTask.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
using DurableClientAttribute = Microsoft.Azure.Functions.Worker.Extensions.DurableTask.DurableClientAttribute;

namespace AzureFunctions.Worker.Extensions.TestHost.Functions;

public sealed class DurableTask(IOptions<JsonOptions> jsonOptions)
{
    [Function(nameof(DurableTask) + "_" + nameof(GetInstances))]
    public async Task<IEnumerable<Orchestration>> GetInstances(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "durable-task/instances")] HttpRequest request,
        [DurableClient] DurableTaskClient durableTaskClient,
        [FromQuery] string? prefix = null)
    {
        return (await durableTaskClient
            .ListInstancesAsync(prefix))
            .Select(orchestration => new Orchestration
            {
                Id = orchestration.OrchestrationInstance.InstanceId,
                OrchestratinFunction = orchestration.Name,
                RuntimeStatus = orchestration.OrchestrationStatus,
                CustomStatus = JsonSerializer
                    .Deserialize<JsonElement>(orchestration.Status, jsonOptions.Value.JsonSerializerOptions),
            });
    }
}

public class Orchestration
{
    public required string Id { get; set; }

    public required string OrchestratinFunction { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<OrchestrationStatus>))]
    public OrchestrationStatus RuntimeStatus { get; set; }

    public JsonElement? CustomStatus { get; set; }
}