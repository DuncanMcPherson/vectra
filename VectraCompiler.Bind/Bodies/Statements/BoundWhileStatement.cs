using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundWhileStatement(
    SourceSpan span,
    BoundExpression condition,
    BoundStatement body) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
    public BoundExpression Condition { get; } = condition;
    public BoundStatement Body { get; } = body;
}