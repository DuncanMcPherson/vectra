using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundForStatement(
    SourceSpan span,
    BoundStatement? initializer,
    BoundExpression? condition,
    BoundExpression? increment,
    BoundStatement body) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.ForStatement;
    public BoundStatement? Initializer { get; } = initializer;
    public BoundExpression? Condition { get; } = condition;
    public BoundExpression? Increment { get; } = increment;
    public BoundStatement Body { get; } = body;
}