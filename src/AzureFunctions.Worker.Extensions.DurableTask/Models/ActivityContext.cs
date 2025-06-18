using Microsoft.DurableTask;

namespace Microsoft.Azure.Functions.Worker;

/// <summary>
/// Activity context stub in order to schedule parameterless activity
/// </summary>
public sealed class ActivityContext : TaskActivityContext
{
    /// <summary>
    /// Default <see cref="TaskActivityContext"/> stub in order to schedule parameterless activity
    /// </summary>
    public static readonly TaskActivityContext Default = new ActivityContext();

    #region Internal

    internal ActivityContext()
    {
    }

    public override TaskName Name => throw new NotSupportedException();

    public override string InstanceId => throw new NotSupportedException();

    #endregion
}
