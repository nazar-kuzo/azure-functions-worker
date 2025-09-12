using DurableTask.Core;
using DurableTask.Core.Exceptions;

namespace DurableTask.Client;

/// <summary>
/// Configuration options for the Durable Task Client extension.
/// </summary>
public class DurableTaskClientOptions
{
    private static readonly OrchestrationStatus[] NonRunningStates =
    [
        OrchestrationStatus.Running, OrchestrationStatus.ContinuedAsNew, OrchestrationStatus.Pending,
    ];

    /// <summary>
    /// Gets or sets the Azure Storage connection string.
    /// </summary>
    [Required]
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the name of the task hub. This value is used to group related storage resources.
    /// </summary>
    [Required]
    public required string TaskHubName { get; set; }

    /// <summary>
    /// When true, will throw an exception when attempting to create an orchestration that already
    /// exists with status not specified in <see cref="OverridableExistingInstanceStates"/>.
    /// </summary>
    public bool ThrowExceptionOnInvalidOverridableStatus { get; set; } = true;

    /// <summary>
    /// Controls the behavior of <see cref="DurableTaskClient.RaiseEventAsync(string, string, object)"/> in situations where the specified orchestration
    /// does not exist, or is not in a running state. If set to true, an exception is thrown. If set to false, the event is silently discarded.
    /// </summary>
    /// <remarks>
    /// The default behavior depends on the selected storage provider.
    /// </remarks>
    public bool ThrowStatusExceptionsOnRaiseEvent { get; set; } = true;

    /// <summary>
    /// Gets or sets the states that allows existing instance to be overridden by a new instance, otherwise an
    /// <see cref="OrchestrationAlreadyExistsException"/> exception is thrown.
    /// </summary>
    public OverridableStates OverridableExistingInstanceStates { get; set; } = OverridableStates.NonRunningStates;

    internal OrchestrationStatus[] StatusesNotToOverride =>
        this.OverridableExistingInstanceStates == OverridableStates.NonRunningStates ? NonRunningStates : [];
}

[OptionsValidator]
public partial class DurableTaskClientOptionsValidator : IValidateOptions<DurableTaskClientOptions>
{
}

/// <summary>
/// Represents options for different states that an existing orchestrator can be in to be able to be overwritten by
/// an attempt to start a new instance with the same instance Id.
/// </summary>
public enum OverridableStates
{
    /// <summary>
    /// Option to start a new orchestrator instance with an existing instance Id when the existing
    /// instance is in any state.
    /// </summary>
    AnyState,

    /// <summary>
    /// Option to only start a new orchestrator instance with an existing instance Id when the existing
    /// instance is in a terminated, failed, or completed state.
    /// </summary>
    NonRunningStates,
}