using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Worker.Extensions.DurableTask.Internal;

internal static class ExpressionExtensions
{
    private static readonly ConcurrentDictionary<MethodInfo, string> FunctionNames = new();

    public static (string FunctionName, object? Input) GetDurableFunctionInfo(this LambdaExpression activityExpression)
    {
        if (activityExpression.Body is MethodCallExpression methodExpression)
        {
            var functionName = FunctionNames.GetOrAdd(
                methodExpression.Method,
                methodInfo => methodInfo.GetCustomAttributes<FunctionAttribute>().First().Name);

            var input = methodExpression.Arguments
                .LastOrDefault()
                ?.GetConstant()
                ?.Value;

            // indicates that activity is called without parameters
            if (input is ActivityContext)
            {
                return (functionName, null);
            }

            // indicates that orchestration is called with implicit input
            if (input is OrchestrationContext orchestrationContext)
            {
                return (functionName, orchestrationContext.Input);
            }

            // indicates that orchestration is called with explicit input
            return (functionName, input);
        }

        throw new InvalidOperationException("Method call expression expected");
    }

    private static ConstantExpression? GetConstant(this Expression expression)
    {
        var evaluatedExpression = new SubtreeEvaluator(expression).Evaluate();

        return evaluatedExpression == null ? null : new DefaultExpressionVisitor()
            .GetConstants(evaluatedExpression)
            .SingleOrDefault();
    }
}
