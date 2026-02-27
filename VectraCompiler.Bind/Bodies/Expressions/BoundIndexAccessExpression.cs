using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundIndexAccessExpression(
    SourceSpan span,
    BoundExpression target,
    BoundExpression index,
    TypeSymbol elementType) : BoundExpression(span, elementType)
{
    public override BoundNodeKind Kind => BoundNodeKind.IndexAccessExpression;
    public BoundExpression Target { get; } = target;
    public BoundExpression Index { get; } = index;
}