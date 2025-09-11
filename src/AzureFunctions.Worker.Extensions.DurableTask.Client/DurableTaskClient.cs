using AzureFunctions.Worker.Extensions.DurableTask.Client.Internal;
using DurableTask.AzureStorage;
using DurableTask.Core;
using DurableTask.Core.Query;

namespace Microsoft.Azure.Functions.Worker.Extensions.DurableTask;

public sealed class DurableTaskClient(
    IOptions<DurableTaskClientOptions> durableTaskClientOptions,
    SystemTextJsonDataConverter dataConverter)
{
    private readonly TaskHubClient taskHubClient = new(
        new AzureStorageOrchestrationService(new()
        {
            StorageAccountClientProvider = new(durableTaskClientOptions.Value.ConnectionString),
            TaskHubName = durableTaskClientOptions.Value.TaskHubName,
            ThrowExceptionOnInvalidDedupeStatus = durableTaskClientOptions.Value.ThrowExceptionOnInvalidOverridableStatus,
        }),
        dataConverter);

    public async Task<IEnumerable<OrchestrationState>> ListInstancesAsync(
        string? instanceIdPrefix = null,
        ICollection<OrchestrationStatus>? runtimeStatuses = null,
        bool fetchInputsAndOutputs = false,
        CancellationToken cancellationToken = default)
    {
        var result = Enumerable.Empty<OrchestrationState>();

        var queryClient = (IOrchestrationServiceQueryClient) this.taskHubClient.ServiceClient;

        var pageResult = default(OrchestrationQueryResult);

        do
        {
            pageResult = await queryClient.GetOrchestrationWithQueryAsync(
                new OrchestrationQuery
                {
                    PageSize = 1000,
                    InstanceIdPrefix = instanceIdPrefix,
                    FetchInputsAndOutputs = fetchInputsAndOutputs,
                    ContinuationToken = pageResult?.ContinuationToken,
                    RuntimeStatus = runtimeStatuses,
                },
                cancellationToken);

            result = result.Concat(pageResult.OrchestrationState);
        }
        while (pageResult.ContinuationToken != null);

        return result;
    }

    public async Task<OrchestrationState?> GetStatusAsync(string instanceId, bool showHistory = false)
    {
        return (await this.taskHubClient.ServiceClient
            .GetOrchestrationStateAsync(instanceId, showHistory))
            .FirstOrDefault();
    }

    public Task<string> StartNewAsync(string orchestratorFunctionName, string? instanceId = null)
    {
        return this.StartNewAsync<object>(orchestratorFunctionName, instanceId, input: null);
    }

    public Task<string> StartNewAsync<T>(string orchestratorFunctionName, T input)
        where T : class
    {
        return this.StartNewAsync<object>(orchestratorFunctionName, instanceId: null, input);
    }

    public async Task<string> StartNewAsync<T>(
        string orchestratorFunctionName,
        string? instanceId,
        T? input)
    {
        var orchestrationInstance = await this.taskHubClient.CreateOrchestrationInstanceAsync(
            orchestratorFunctionName,
            version: string.Empty,
            instanceId,
            input,
            tags: null,
            durableTaskClientOptions.Value.StatusesNotToOverride);

        return orchestrationInstance.InstanceId;
    }

    public Task<OrchestrationState> WaitForOrchestrationAsync(string instanceId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return this.taskHubClient.WaitForOrchestrationAsync(new OrchestrationInstance { InstanceId = instanceId }, timeout, cancellationToken);
    }

    public Task TerminateAsync(string instanceId, string reason)
    {
        return this.taskHubClient.TerminateInstanceAsync(new OrchestrationInstance { InstanceId = instanceId }, reason);
    }

    public async Task RaiseEventAsync(string instanceId, string eventName, object? eventData = null)
    {
        if (durableTaskClientOptions.Value.ThrowStatusExceptionsOnRaiseEvent)
        {
            var orchestration = await GetOrchestrationInstanceStateAsync(instanceId);

            if (orchestration == null)
            {
                return;
            }

            if (orchestration.IsRunning())
            {
                // External events are not supposed to target any particular execution ID.
                // We need to clear it to avoid sending messages to an expired ContinueAsNew instance.
                orchestration.OrchestrationInstance.ExecutionId = null;

                await this.taskHubClient.RaiseEventAsync(orchestration.OrchestrationInstance, eventName, eventData!);
            }
            else
            {
                throw new InvalidOperationException($"Cannot raise event {eventName} " +
                    $"for orchestration instance {instanceId} " +
                    $"because instance is in {orchestration.OrchestrationStatus} state");
            }
        }
        else
        {
            // fast path: always raise the event (it will be silently discarded if the instance does not exist or is not running)
            await this.taskHubClient.RaiseEventAsync(new OrchestrationInstance() { InstanceId = instanceId }, eventName, eventData!);
        }

        async Task<OrchestrationState> GetOrchestrationInstanceStateAsync(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                throw new ArgumentNullException(nameof(instanceId));
            }

            var orchestration = await this.taskHubClient.GetOrchestrationStateAsync(instanceId);

            if (orchestration?.OrchestrationInstance == null)
            {
                throw new ArgumentException($"No instance with ID '{instanceId}' was found.", nameof(instanceId));
            }

            return orchestration;
        }
    }
}
