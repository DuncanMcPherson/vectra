using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundCallExpression(SourceSpan span, MethodSymbol method, BoundExpression? receiver, IReadOnlyList<BoundExpression> arguments) 
    : BoundExpression(span, method.ReturnType)
{
    public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
    public MethodSymbol Method { get; } = method;
    public BoundExpression? Receiver { get; } = receiver;
    public IReadOnlyList<BoundExpression> Arguments { get; } = arguments.ToArray();
}