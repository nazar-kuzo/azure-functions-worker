using System.Linq.Expressions;

namespace AzureFunctions.Worker.Extensions.DurableTask.Internal;

/// <summary>
/// Default expression visitor that collects all constant expressions in the expression tree.
/// </summary>
internal sealed class DefaultExpressionVisitor : ExpressionVisitor
{
    private List<ConstantExpression> constants = [];

    public List<ConstantExpression> GetConstants(Expression expression)
    {
        this.constants = [];

        this.Visit(expression);

        return this.constants;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        this.constants.Add(node);

        return base.VisitConstant(node);
    }
}
