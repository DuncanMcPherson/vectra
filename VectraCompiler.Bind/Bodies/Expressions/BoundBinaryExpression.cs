using VectraCompiler.AST.Models;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundBinaryExpression(SourceSpan span, BoundExpression left, BoundBinaryOperator op, BoundExpression right)
    : BoundExpression(span, op.ResultType)
{
    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
    public BoundExpression Left { get; } = left;
    public BoundBinaryOperator Operator { get; } = op;
    public BoundExpression Right { get; } = right;
}