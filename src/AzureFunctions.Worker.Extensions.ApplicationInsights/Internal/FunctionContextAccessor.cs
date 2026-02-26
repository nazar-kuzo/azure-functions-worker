namespace Microsoft.Azure.Functions.Worker;

internal sealed class FunctionContextAccessor : IFunctionContextAccessor
{
    private static readonly AsyncLocal<FunctionContextHolder> ContextHolder = new();

    public FunctionContext? FunctionContext
    {
        get => ContextHolder.Value?.Context;

        set
        {
            // clear current FunctionContext trapped in the AsyncLocals, as its done.
            ContextHolder.Value?.Context = null;

            if (value != null)
            {
                // Uuse an object indirection to hold the FunctionContext in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                ContextHolder.Value = new FunctionContextHolder { Context = value };
            }
        }
    }

    private sealed class FunctionContextHolder
    {
        public FunctionContext? Context;
    }
}

/// <summary>
/// Provides access to the current <see cref="FunctionContext"/>, if one is available.
/// </summary>
public interface IFunctionContextAccessor
{
    /// <summary>
    /// Gets the current <see cref="FunctionContext"/>.
    /// Returns <see langword="null" /> if there is no active <see cref="FunctionContext" />.
    /// </summary>
    FunctionContext? FunctionContext { get; internal set; }
}