using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public class BoundNewExpression(SourceSpan span, NamedTypeSymbol constructedType, ConstructorSymbol ctor, IReadOnlyList<BoundExpression> arguments)
    : BoundExpression(span, constructedType)
{
    public override BoundNodeKind Kind => BoundNodeKind.NewExpression;
    public ConstructorSymbol Constructor { get; } = ctor;
    public IReadOnlyList<BoundExpression> Arguments { get; } = arguments;
}