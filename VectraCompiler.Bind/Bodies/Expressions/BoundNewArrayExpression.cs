using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundNewArrayExpression(
    SourceSpan span,
    ArrayTypeSymbol arrayType,
    BoundExpression count) : BoundExpression(span, arrayType)
{
    public override BoundNodeKind Kind => BoundNodeKind.NewArrayExpression;
    public ArrayTypeSymbol ArrayType { get; } = arrayType;
    public BoundExpression Count { get; } = count;
}