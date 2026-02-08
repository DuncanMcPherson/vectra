using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundLocalExpression(SourceSpan span, LocalSymbol local) : BoundExpression(span, local.Type)
{
    public override BoundNodeKind Kind => BoundNodeKind.LocalExpression;
    public LocalSymbol Local { get; } = local;
}