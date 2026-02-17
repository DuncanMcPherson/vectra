using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public abstract class BoundExpression(SourceSpan span, TypeSymbol type) : BoundNode(span)
{
    public TypeSymbol Type { get; } = type;
}