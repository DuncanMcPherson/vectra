using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundLocalExpression(SourceSpan span, VariableSymbol local) : BoundExpression(span, local.Type)
{
    public override BoundNodeKind Kind => BoundNodeKind.LocalExpression;
    public VariableSymbol Local { get; } = local;
}