using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Bodies.Expressions;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundExpressionStatement(SourceSpan span, BoundExpression expression) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;
    public BoundExpression Expression { get; } = expression;
}