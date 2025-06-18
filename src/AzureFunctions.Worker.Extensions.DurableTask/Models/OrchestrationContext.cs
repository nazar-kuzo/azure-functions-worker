using Microsoft.DurableTask;

namespace Microsoft.Azure.Functions.Worker;

/// <summary>
/// Orchestration context stub in order to schedule parameterized orchestration
/// </summary>
public sealed class OrchestrationContext : TaskOrchestrationContext
{
    /// <summary>
    /// Default <see cref="TaskOrchestrationContext"/> stub in order to schedule parameterless activity
    /// </summary>
    public static readonly TaskOrchestrationContext Default = new OrchestrationContext();

    internal object? Input { get; set; }

    /// <summary>
    /// Creates <see cref="TaskOrchestrationContext"/> stub with 1 parameter
    /// </summary>
    /// <typeparam name="T1">First parameter type</typeparam>
    /// <param name="item1">First parameter</param>
    /// <returns>Orchestration context stub</returns>
    public static TaskOrchestrationContext Create<T1>(T1 item1)
    {
        return new OrchestrationContext { Input = item1 };
    }

    /// <summary>
    /// Creates <see cref="TaskOrchestrationContext"/> stub with 2 parameters
    /// </summary>
    /// <typeparam name="T1">First parameter type</typeparam>
    /// <typeparam name="T2">Second parameter type</typeparam>
    /// <param name="item1">First parameter</param>
    /// <param name="item2">Second parameter</param>
    /// <returns>Orchestration context stub</returns>
    public static TaskOrchestrationContext Create<T1, T2>(
        T1 item1, T2 item2)
    {
        return new OrchestrationContext { Input = (item1, item2) };
    }

    /// <summary>
    /// Creates <see cref="TaskOrchestrationContext"/> stub with 3 parameters
    /// </summary>
    /// <typeparam name="T1">First parameter type</typeparam>
    /// <typeparam name="T2">Second parameter type</typeparam>
    /// <typeparam name="T3">Third parameter type</typeparam>
    /// <param name="item1">First parameter</param>
    /// <param name="item2">Second parameter</param>
    /// <param name="item3">Third parameter</param>
    /// <returns>Orchestration context stub</returns>
    public static TaskOrchestrationContext Create<T1, T2, T3>(
        T1 item1, T2 item2, T3 item3)
    {
        return new OrchestrationContext { Input = (item1, item2, item3) };
    }

    /// <summary>
    /// Creates <see cref="TaskOrchestrationContext"/> stub with 4 parameters
    /// </summary>
    /// <typeparam name="T1">First parameter type</typeparam>
    /// <typeparam name="T2">Second parameter type</typeparam>
    /// <typeparam name="T3">Third parameter type</typeparam>
    /// <typeparam name="T4">Forth parameter type</typeparam>
    /// <param name="item1">First parameter</param>
    /// <param name="item2">Second parameter</param>
    /// <param name="item3">Third parameter</param>
    /// <param name="item4">Forth parameter</param>
    /// <returns>Orchestration context stub</returns>
    public static TaskOrchestrationContext Create<T1, T2, T3, T4>(
        T1 item1, T2 item2, T3 item3, T4 item4)
    {
        return new OrchestrationContext { Input = (item1, item2, item3, item4) };
    }

    #region Internal

    private OrchestrationContext()
    {
    }

    public override TaskName Name => throw new NotImplementedException();

    public override string InstanceId => throw new NotImplementedException();

    public override ParentOrchestrationInstance? Parent => throw new NotImplementedException();

    public override DateTime CurrentUtcDateTime => throw new NotImplementedException();

    public override bool IsReplaying => throw new NotImplementedException();

    protected override ILoggerFactory LoggerFactory => throw new NotImplementedException();

    public override Task<TResult> CallActivityAsync<TResult>(TaskName name, object? input = null, TaskOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public override Task<TResult> CallSubOrchestratorAsync<TResult>(TaskName orchestratorName, object? input = null, TaskOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public override void ContinueAsNew(object? newInput = null, bool preserveUnprocessedEvents = true)
    {
        throw new NotImplementedException();
    }

    public override Task CreateTimer(DateTime fireAt, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override T? GetInput<T>()
        where T : default
    {
        throw new NotImplementedException();
    }

    public override Guid NewGuid()
    {
        throw new NotImplementedException();
    }

    public override void SendEvent(string instanceId, string eventName, object payload)
    {
        throw new NotImplementedException();
    }

    public override void SetCustomStatus(object? customStatus)
    {
        throw new NotImplementedException();
    }

    public override Task<T> WaitForExternalEvent<T>(string eventName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion
}
