using System.Linq.Expressions;

namespace AzureFunctions.Worker.Extensions.DurableTask.Internal;

/// <summary>
/// Evaluates and replaces sub-trees when first candidate is reached (top-down)
/// </summary>
internal sealed class SubtreeEvaluator(Expression expression) : ExpressionVisitor
{
    private readonly HashSet<Expression> candidates =
        new Nominator(expression, exp => exp.NodeType != ExpressionType.Parameter).Nominate();

    public override Expression? Visit(Expression? node)
    {
        if (node == null)
        {
            return null;
        }

        if (this.candidates.Contains(node))
        {
            if (node.NodeType == ExpressionType.Constant)
            {
                return node;
            }
            else
            {
                var evaluatedResult = Expression
                    .Lambda(node)
                    .Compile()
                    .DynamicInvoke(null);

                return Expression.Constant(evaluatedResult, node.Type);
            }
        }

        try
        {
            return base.Visit(node);
        }
        catch
        {
            return node;
        }
    }

    public Expression? Evaluate()
    {
        return this.Visit(expression);
    }
}
