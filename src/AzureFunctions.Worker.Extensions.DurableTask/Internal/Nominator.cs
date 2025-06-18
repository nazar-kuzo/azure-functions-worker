using System.Linq.Expressions;

namespace AzureFunctions.Worker.Extensions.DurableTask.Internal;

/// <summary>
/// Performs bottom-up analysis to determine which nodes can possibly
/// be part of an evaluated sub-tree.
/// </summary>
internal sealed class Nominator(Expression expression, Predicate<Expression> shouldEvaluate) : ExpressionVisitor
{
    private readonly HashSet<Expression> candidates = [];

    private bool canBeEvaluated;

    public override Expression? Visit(Expression? node)
    {
        if (node != null)
        {
            var saveCanBeEvaluated = this.canBeEvaluated;

            this.canBeEvaluated = true;

            base.Visit(node);

            if (this.canBeEvaluated)
            {
                if (shouldEvaluate(node))
                {
                    this.candidates.Add(node);
                }
                else
                {
                    this.canBeEvaluated = false;
                }
            }

            this.canBeEvaluated |= saveCanBeEvaluated;
        }

        return node;
    }

    public HashSet<Expression> Nominate()
    {
        this.Visit(expression);

        return this.candidates;
    }
}
