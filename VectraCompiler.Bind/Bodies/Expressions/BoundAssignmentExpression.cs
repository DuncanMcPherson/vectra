using VectraCompiler.AST.Models;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundAssignmentExpression(SourceSpan span, BoundExpression target, BoundExpression value)
    : BoundExpression(span, target.Type)
{
    public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
    public BoundExpression Target { get; } = target;
    public BoundExpression Value { get; } = value;
}