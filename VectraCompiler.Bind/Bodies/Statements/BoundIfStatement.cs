using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundIfStatement(
    SourceSpan span,
    BoundExpression condition,
    BoundStatement thenBranch,
    BoundStatement? elseBranch) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
    public BoundExpression Condition { get; } = condition;
    public BoundStatement ThenBranch { get; } = thenBranch;
    public BoundStatement? ElseBranch { get; } = elseBranch;
}