using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public class BoundUnaryExpression(
    SourceSpan span,
    BoundUnaryOperator op,
    BoundExpression operand)
: BoundExpression(span, operand.Type)
{
    public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
    public BoundUnaryOperator Operator { get; } = op;
    public BoundExpression Operand { get; } = operand;
}