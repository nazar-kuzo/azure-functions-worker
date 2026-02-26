using System.Linq.Expressions;
using Microsoft.DurableTask;

namespace Microsoft.Azure.Functions.Worker;

/// <summary>
/// Extensions for enforcing compile-time type safety when working with <see cref="TaskOrchestrationContext"/>
/// </summary>
public static class TaskOrchestrationContextExtensions
{
    /// <summary>
    /// Retries activity until condition is met
    /// </summary>
    /// <typeparam name="TResult">Activity result type</typeparam>
    /// <param name="context">TaskOrchestrationContext</param>
    /// <param name="activityExpression">Expression to call Activity</param>
    /// <param name="exitPredicate">Predicate when retry logic should run until</param>
    /// <param name="expiryTime">Absolute expiration for retry</param>
    /// <param name="retryDelay">Delay between retries, Default - 30 seconds</param>
    /// <param name="initialDelay">Initial delay before first action execution</param>
    /// <returns>Activity result</returns>
    public static Task<TResult> RetryActivityUntil<TResult>(
        this TaskOrchestrationContext context,
        Expression<Func<Task<TResult>>> activityExpression,
        Func<TResult, bool> exitPredicate,
        DateTime expiryTime,
        TimeSpan? retryDelay = null,
        TimeSpan? initialDelay = null)
    {
        return RetryActivityUntilInternal(context, activityExpression, exitPredicate, expiryTime, retryDelay, initialDelay);
    }

    /// <summary>
    /// Retries activity until condition is met
    /// </summary>
    /// <typeparam name="T">Durable Function type</typeparam>
    /// <typeparam name="TResult">Activity result type</typeparam>
    /// <param name="context">TaskOrchestrationContext</param>
    /// <param name="activityExpression">Expression to call Activity</param>
    /// <param name="exitPredicate">Predicate when retry logic should run until</param>
    /// <param name="expiryTime">Absolute expiration for retry</param>
    /// <param name="retryDelay">Delay between retries, Default - 30 seconds</param>
    /// <param name="initialDelay">Initial delay before first action execution</param>
    /// <returns>Activity result</returns>
    public static Task<TResult> RetryActivityUntil<T, TResult>(
        this TaskOrchestrationContext context,
        Expression<Func<T, Task<TResult>>> activityExpression,
        Func<TResult, bool> exitPredicate,
        DateTime expiryTime,
        TimeSpan? retryDelay = null,
        TimeSpan? initialDelay = null)
    {
        return RetryActivityUntilInternal(context, activityExpression, exitPredicate, expiryTime, retryDelay, initialDelay);
    }

    /// <summary>
    /// Asynchronously invokes an activity by expression reference and with the specified input value.
    /// </summary>
    /// <param name="context">Orchestration context</param>
    /// <param name="activityExpression">Lambda expression that calls activity function</param>
    /// <param name="taskOptions">Additional options that control the execution and processing of the activity.</param>
    /// <returns>A task that completes when the activity completes or fails.</returns>
    public static Task CallActivityAsync(
        this TaskOrchestrationContext context,
        Expression<Func<Task>> activityExpression,
        TaskOptions? taskOptions = null)
    {
        var (activityName, input) = activityExpression.GetDurableFunctionInfo();

        return context.CallActivityAsync(activityName, input, taskOptions);
    }

    /// <summary>
    /// Asynchronously invokes an activity by expression reference and with the specified input value.
    /// </summary>
    /// <typeparam name="T">DurableFunction class type that contains activity function</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="activityExpression">Lambda expression that calls activity function</param>
    /// <param name="taskOptions">Additional options that control the execution and processing of the activity.</param>
    /// <returns>A task that completes when the activity completes or fails.</returns>
    public static Task CallActivityAsync<T>(
        this TaskOrchestrationContext context,
        Expression<Func<T, Task>> activityExpression,
        TaskOptions? taskOptions = null)
    {
        var (activityName, input) = activityExpression.GetDurableFunctionInfo();

        return context.CallActivityAsync(activityName, input, taskOptions);
    }

    /// <summary>
    /// Asynchronously invokes an activity by expression reference and with the specified input value.
    /// </summary>
    /// <typeparam name="TResult">The type into which to deserialize the activity's output.</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="activityExpression">Lambda expression that calls activity function</param>
    /// <param name="taskOptions">Additional options that control the execution and processing of the activity.</param>
    /// <returns>A task that completes when the activity completes or fails.
    /// The result of the task is the activity's return value.</returns>
    public static Task<TResult> CallActivityAsync<TResult>(
        this TaskOrchestrationContext context,
        Expression<Func<Task<TResult>>> activityExpression,
        TaskOptions? taskOptions = null)
    {
        var (activityName, input) = activityExpression.GetDurableFunctionInfo();

        return context.CallActivityAsync<TResult>(activityName, input, taskOptions);
    }

    /// <summary>
    /// Asynchronously invokes an activity by expression reference and with the specified input value.
    /// </summary>
    /// <typeparam name="T">DurableFunction class type that contains activity function</typeparam>
    /// <typeparam name="TResult">The type into which to deserialize the activity's output.</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="activityExpression">Lambda expression that calls activity function</param>
    /// <param name="taskOptions">Additional options that control the execution and processing of the activity.</param>
    /// <returns>A task that completes when the activity completes or fails.
    /// The result of the task is the activity's return value.</returns>
    public static Task<TResult> CallActivityAsync<T, TResult>(
        this TaskOrchestrationContext context,
        Expression<Func<T, Task<TResult>>> activityExpression,
        TaskOptions? taskOptions = null)
    {
        var (activityName, input) = activityExpression.GetDurableFunctionInfo();

        return context.CallActivityAsync<TResult>(activityName, input, taskOptions);
    }

    /// <summary>
    /// Executes a named sub-orchestrator.
    /// </summary>
    /// <param name="context">Orchestration context</param>
    /// <param name="orchestratorExpression">Lambda expression that calls orchestrator function</param>
    /// <param name="options">Additional options that control the execution and processing of the sub-orchestrator.</param>
    /// <returns>A task that completes when the sub-orchestrator completes or fails.</returns>
    public static Task CallSubOrchestratorAsync(
        this TaskOrchestrationContext context,
        Expression<Func<Task>> orchestratorExpression,
        SubOrchestrationOptions? options = null)
    {
        return CallSubOrchestratorAsyncInternal(context, orchestratorExpression, options);
    }

    /// <summary>
    /// Executes a named sub-orchestrator.
    /// </summary>
    /// <typeparam name="T">DurableFunction class type that contains sub-orchestrator function</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="orchestratorExpression">Lambda expression that calls orchestrator function</param>
    /// <param name="options">Additional options that control the execution and processing of the sub-orchestrator.</param>
    /// <returns>A task that completes when the sub-orchestrator completes or fails.</returns>
    public static Task CallSubOrchestratorAsync<T>(
        this TaskOrchestrationContext context,
        Expression<Func<T, Task>> orchestratorExpression,
        SubOrchestrationOptions? options = null)
    {
        return CallSubOrchestratorAsyncInternal(context, orchestratorExpression, options);
    }

    /// <summary>
    /// Executes a named sub-orchestrator.
    /// </summary>
    /// <typeparam name="TResult">The type into which to deserialize the activity's output.</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="orchestratorExpression">Lambda expression that calls orchestrator function</param>
    /// <param name="options">Additional options that control the execution and processing of the sub-orchestrator.</param>
    /// <returns>A task with the orchestration result that completes when the sub-orchestrator completes or fails.</returns>
    public static Task<TResult> CallSubOrchestratorAsync<TResult>(
        this TaskOrchestrationContext context,
        Expression<Func<Task<TResult>>> orchestratorExpression,
        SubOrchestrationOptions? options = null)
    {
        return CallSubOrchestratorAsyncInternal<TResult>(context, orchestratorExpression, options);
    }

    /// <summary>
    /// Executes a named sub-orchestrator.
    /// </summary>
    /// <typeparam name="T">DurableFunction class type that contains sub-orchestrator function</typeparam>
    /// <typeparam name="TResult">The type into which to deserialize the activity's output.</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="orchestratorExpression">Lambda expression that calls orchestrator function</param>
    /// <param name="options">Additional options that control the execution and processing of the sub-orchestrator.</param>
    /// <returns>A task with the orchestration result that completes when the sub-orchestrator completes or fails.</returns>
    public static Task<TResult> CallSubOrchestratorAsync<T, TResult>(
        this TaskOrchestrationContext context,
        Expression<Func<T, Task<TResult>>> orchestratorExpression,
        SubOrchestrationOptions? options = null)
    {
        return CallSubOrchestratorAsyncInternal<TResult>(context, orchestratorExpression, options);
    }

    /// <summary>
    /// Executes a named sub-orchestrator.
    /// </summary>
    /// <param name="context">Orchestration context</param>
    /// <param name="orchestratorExpression">Lambda expression that calls orchestrator function</param>
    /// <param name="instanceId">The sub-orchestration instance ID.</param>
    /// <returns>A task that completes when the sub-orchestrator completes or fails.</returns>
    public static Task CallSubOrchestratorAsync(
        this TaskOrchestrationContext context,
        Expression<Func<Task>> orchestratorExpression,
        string instanceId)
    {
        return CallSubOrchestratorAsyncInternal(context, orchestratorExpression, new(instanceId: instanceId));
    }

    /// <summary>
    /// Executes a named sub-orchestrator.
    /// </summary>
    /// <typeparam name="T">DurableFunction class type that contains sub-orchestrator function</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="orchestratorExpression">Lambda expression that calls orchestrator function</param>
    /// <param name="instanceId">The sub-orchestration instance ID.</param>
    /// <returns>A task that completes when the sub-orchestrator completes or fails.</returns>
    public static Task CallSubOrchestratorAsync<T>(
        this TaskOrchestrationContext context,
        Expression<Func<T, Task>> orchestratorExpression,
        string instanceId)
    {
        return CallSubOrchestratorAsyncInternal(context, orchestratorExpression, new(instanceId: instanceId));
    }

    /// <summary>
    /// Executes a named sub-orchestrator.
    /// </summary>
    /// <typeparam name="TResult">The type into which to deserialize the activity's output.</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="orchestratorExpression">Lambda expression that calls orchestrator function</param>
    /// <param name="instanceId">The sub-orchestration instance ID.</param>
    /// <returns>A task with the orchestration result that completes when the sub-orchestrator completes or fails.</returns>
    public static Task<TResult> CallSubOrchestratorAsync<TResult>(
        this TaskOrchestrationContext context,
        Expression<Func<Task<TResult>>> orchestratorExpression,
        string instanceId)
    {
        return CallSubOrchestratorAsyncInternal<TResult>(context, orchestratorExpression, new(instanceId: instanceId));
    }

    /// <summary>
    /// Executes a named sub-orchestrator.
    /// </summary>
    /// <typeparam name="T">DurableFunction class type that contains sub-orchestrator function</typeparam>
    /// <typeparam name="TResult">The type into which to deserialize the activity's output.</typeparam>
    /// <param name="context">Orchestration context</param>
    /// <param name="orchestratorExpression">Lambda expression that calls orchestrator function</param>
    /// <param name="instanceId">The sub-orchestration instance ID.</param>
    /// <returns>A task with the orchestration result that completes when the sub-orchestrator completes or fails.</returns>
    public static Task<TResult> CallSubOrchestratorAsync<T, TResult>(
        this TaskOrchestrationContext context,
        Expression<Func<T, Task<TResult>>> orchestratorExpression,
        string instanceId)
    {
        return CallSubOrchestratorAsyncInternal<TResult>(context, orchestratorExpression, new(instanceId: instanceId));
    }

    private static Task CallSubOrchestratorAsyncInternal(
        TaskOrchestrationContext context,
        LambdaExpression orchestratorExpression,
        SubOrchestrationOptions? options = null)
    {
        var (orchestratorName, input) = orchestratorExpression.GetDurableFunctionInfo();

        return context.CallSubOrchestratorAsync(orchestratorName, input, options);
    }

    private static Task<T> CallSubOrchestratorAsyncInternal<T>(
        TaskOrchestrationContext context,
        LambdaExpression orchestratorExpression,
        SubOrchestrationOptions? options = null)
    {
        var (orchestratorName, input) = orchestratorExpression.GetDurableFunctionInfo();

        return context.CallSubOrchestratorAsync<T>(orchestratorName, input, options);
    }

    private static async Task<TResult> RetryActivityUntilInternal<TResult>(
        this TaskOrchestrationContext context,
        LambdaExpression activityExpression,
        Func<TResult, bool> exitPredicate,
        DateTime expiryTime,
        TimeSpan? retryDelay = null,
        TimeSpan? initialDelay = null)
    {
        retryDelay ??= TimeSpan.FromSeconds(30);

        if (initialDelay.HasValue)
        {
            await context.CreateTimer(context.CurrentUtcDateTime.Add(initialDelay.Value), CancellationToken.None);
        }

        var (activityName, input) = activityExpression.GetDurableFunctionInfo();

        TResult result;

        while (true)
        {
            result = await context.CallActivityAsync<TResult>(activityName, input);

            if (context.CurrentUtcDateTime >= expiryTime || exitPredicate(result))
            {
                break;
            }

            await context.CreateTimer(context.CurrentUtcDateTime.Add(retryDelay.Value), CancellationToken.None);
        }

        return result;
    }
}
