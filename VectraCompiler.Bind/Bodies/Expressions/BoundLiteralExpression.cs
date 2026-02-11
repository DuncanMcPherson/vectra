using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundLiteralExpression(SourceSpan span, object? value, TypeSymbol type) : BoundExpression(span, type)
{
    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
    public object? Value { get; } = value;
}