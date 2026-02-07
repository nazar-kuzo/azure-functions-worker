using DurableTask.Client;
using DurableTask.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Functions.Worker;
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
            .Select(orchestration => Orchestration.FromOrchestrationState(orchestration, jsonOptions.Value.JsonSerializerOptions));
    }

    [Function(nameof(DurableTask) + "_" + nameof(SartNewInstance))]
    public async Task<Orchestration> SartNewInstance(
        [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "durable-task/instances")] HttpRequest request,
        [DurableClient] DurableTaskClient durableTaskClient,
        [FromQuery, Required] string orchestratorFunctionName = "DurableTasks_Orchestration",
        [FromQuery] string? instanceId = null,
        [FromQuery] string? version = null,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement? input = null)
    {
        input ??= JsonElement.Parse(""""
        {
            "name": "Name",
            "email": "some@email.com"
        }
        """");

        instanceId = await durableTaskClient.StartNewAsync(orchestratorFunctionName, instanceId, input, version);

        var orchestration = await durableTaskClient.GetStatusAsync(instanceId);

        return Orchestration.FromOrchestrationState(orchestration!, jsonOptions.Value.JsonSerializerOptions);
    }
}

public class Orchestration
{
    public required string Id { get; set; }

    public required string OrchestratinFunction { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<OrchestrationStatus>))]
    public OrchestrationStatus RuntimeStatus { get; set; }

    public JsonElement? CustomStatus { get; set; }

    public static Orchestration FromOrchestrationState(
        OrchestrationState orchestration,
        JsonSerializerOptions serializerOptions)
    {
        return new Orchestration
        {
            Id = orchestration.OrchestrationInstance.InstanceId,
            OrchestratinFunction = orchestration.Name,
            RuntimeStatus = orchestration.OrchestrationStatus,
            CustomStatus = orchestration.Status == null
                ? null
                : JsonSerializer.Deserialize<JsonElement>(orchestration.Status, serializerOptions),
        };
    }
}