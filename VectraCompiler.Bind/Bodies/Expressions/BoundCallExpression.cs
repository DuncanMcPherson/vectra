using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundCallExpression(SourceSpan span, CallableSymbol method, BoundExpression? receiver, IReadOnlyList<BoundExpression> arguments) 
    : BoundExpression(span, method.ReturnType)
{
    public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
    public CallableSymbol Method { get; } = method;
    public BoundExpression? Receiver { get; } = receiver;
    public IReadOnlyList<BoundExpression> Arguments { get; } = arguments.ToArray();
}