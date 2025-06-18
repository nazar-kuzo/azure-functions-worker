using Microsoft.DurableTask.Client;

namespace Microsoft.Azure.Functions.Worker;

/// <summary>
/// Orchestration metadata extensions to perform common checks with less boilerplate
/// </summary>
public static class OrchestrationMetadataExtensions
{
    /// <summary>
    /// Indicates whether orchestration is completed.
    /// </summary>
    /// <param name="orchestration">Orchestration metadata</param>
    /// <returns>True when orchestration does not exist, or runtime status is
    /// <see cref="OrchestrationRuntimeStatus.Completed"/> or <see cref="OrchestrationRuntimeStatus.Failed"/></returns>
    public static bool IsCompleted(this OrchestrationMetadata orchestration)
    {
        return orchestration == null ||
            orchestration.RuntimeStatus == OrchestrationRuntimeStatus.Completed ||
            orchestration.RuntimeStatus == OrchestrationRuntimeStatus.Failed;
    }

    /// <summary>
    /// Indicates whether orchestration is failed.
    /// </summary>
    /// <param name="orchestration">Orchestration metadata</param>
    /// <returns>True when orchestration exists and runtime status is <see cref="OrchestrationRuntimeStatus.Failed"/></returns>
    public static bool IsFailed(this OrchestrationMetadata orchestration)
    {
        return orchestration?.RuntimeStatus == OrchestrationRuntimeStatus.Failed;
    }
}
