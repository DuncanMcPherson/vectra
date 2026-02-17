using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundReturnStatement(SourceSpan span, BoundExpression? expression) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
    public BoundExpression? Expression { get; } = expression;
}