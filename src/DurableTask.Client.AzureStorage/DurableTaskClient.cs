using System.Diagnostics;
using DurableTask.AzureStorage;
using DurableTask.Core;
using DurableTask.Core.History;
using DurableTask.Core.Query;

namespace DurableTask.Client;

public sealed class DurableTaskClient
{
    public DurableTaskClient(
        IOptions<DurableTaskClientOptions> durableTaskClientOptions,
        SystemTextJsonDataConverter dataConverter)
    {
        this.durableTaskClientOptions = durableTaskClientOptions;
        this.dataConverter = dataConverter;
        this.serviceClient = new AzureStorageOrchestrationService(new()
        {
            StorageAccountClientProvider = !string.IsNullOrEmpty(this.durableTaskClientOptions.Value.AccountName)
                ? new(durableTaskClientOptions.Value.AccountName!, durableTaskClientOptions.Value.TokenCredential!)
                : new(durableTaskClientOptions.Value.ConnectionString!),
            TaskHubName = durableTaskClientOptions.Value.TaskHubName,
            ThrowExceptionOnInvalidDedupeStatus = durableTaskClientOptions.Value.ThrowExceptionOnInvalidOverridableStatus,
        });
        this.taskHubClient = new(this.serviceClient, dataConverter);
    }

    private readonly IOptions<DurableTaskClientOptions> durableTaskClientOptions;
    private readonly SystemTextJsonDataConverter dataConverter;
    private readonly IOrchestrationServiceClient serviceClient;
    private readonly TaskHubClient taskHubClient;

    public async Task<IEnumerable<OrchestrationState>> ListInstancesAsync(
        string? instanceIdPrefix = null,
        ICollection<OrchestrationStatus>? runtimeStatuses = null,
        bool fetchInputsAndOutputs = false,
        CancellationToken cancellationToken = default)
    {
        var result = Enumerable.Empty<OrchestrationState>();

        var pageResult = default(OrchestrationQueryResult);

        do
        {
            pageResult = await ((IOrchestrationServiceQueryClient) this.serviceClient).GetOrchestrationWithQueryAsync(
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
        T? input,
        string? version = null)
    {
        var orchestrationInstance = new OrchestrationInstance
        {
            InstanceId = instanceId ?? Guid.NewGuid().ToString("N"),
            ExecutionId = Guid.NewGuid().ToString("N"),
        };

        var inputJson = input is null ? null : this.dataConverter.Serialize(input);

        await this.serviceClient.CreateTaskOrchestrationAsync(
            new TaskMessage
            {
                OrchestrationInstance = orchestrationInstance,
                Event = new ExecutionStartedEvent(-1, inputJson)
                {
                    Name = orchestratorFunctionName,
                    OrchestrationInstance = orchestrationInstance,
                    Version = version ?? string.Empty,
                    ParentTraceContext = Activity.Current?.Id == null
                        ? null
                        : new(Activity.Current.Id, Activity.Current.TraceStateString),
                },
            },
            [.. this.durableTaskClientOptions.Value.StatusesNotToOverride]);

        return orchestrationInstance.InstanceId;
    }

    public async Task<OrchestrationState?> WaitForOrchestrationAsync(
        string instanceId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        return await this.taskHubClient.WaitForOrchestrationAsync(
            new OrchestrationInstance { InstanceId = instanceId },
            timeout ?? Timeout.InfiniteTimeSpan,
            cancellationToken);
    }

    public Task TerminateAsync(string instanceId, string reason)
    {
        return this.taskHubClient.TerminateInstanceAsync(new OrchestrationInstance { InstanceId = instanceId }, reason);
    }

    public async Task RaiseEventAsync(string instanceId, string eventName, object? eventData = null)
    {
        if (this.durableTaskClientOptions.Value.ThrowStatusExceptionsOnRaiseEvent)
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
