using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundThrowStatement(SourceSpan span, BoundExpression expression) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.ThrowStatement;
    public BoundExpression Expression { get; } = expression;
}