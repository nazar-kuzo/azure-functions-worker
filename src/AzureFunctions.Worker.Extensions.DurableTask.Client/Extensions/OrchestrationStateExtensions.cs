using DurableTask.Core;

namespace Microsoft.Azure.Functions.Worker.Extensions.DurableTask;

public static class OrchestrationStateExtensions
{
    /// <summary>
    /// Determines whether the specified orchestration is running.
    /// </summary>
    /// <param name="state">Orchestration state</param>
    /// <returns>Whether the specified orchestration is running</returns>
    public static bool IsRunning(this OrchestrationState state)
    {
        return state.OrchestrationStatus == OrchestrationStatus.Running;
    }

    /// <summary>
    /// Determines whether the specified orchestration is running.
    /// </summary>
    /// <param name="state">Orchestration state</param>
    /// <returns>Whether the specified orchestration is running</returns>
    public static bool IsCompleted(this OrchestrationState state)
    {
        return state.OrchestrationStatus == OrchestrationStatus.Completed ||
            state.OrchestrationStatus == OrchestrationStatus.Failed ||
            state.OrchestrationStatus == OrchestrationStatus.Terminated;
    }
}
