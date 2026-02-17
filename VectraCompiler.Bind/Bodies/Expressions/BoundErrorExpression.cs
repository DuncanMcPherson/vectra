using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public class BoundErrorExpression(SourceSpan span, TypeSymbol type) : BoundExpression(span, type)
{
    public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;
}