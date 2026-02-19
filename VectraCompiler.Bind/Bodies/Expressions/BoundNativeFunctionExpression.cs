using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public sealed class BoundNativeFunctionExpression(SourceSpan span, NativeFunctionSymbol native)
    : BoundExpression(span, native.ReturnType)
{
    public override BoundNodeKind Kind => BoundNodeKind.NativeFunctionExpression;
    public NativeFunctionSymbol Native { get; } = native;
}

public sealed class BoundNativeFunctionCallExpression(SourceSpan span, NativeFunctionSymbol nativeFunction, BoundExpression[] arguments)
    : BoundExpression(span, nativeFunction.ReturnType)
{
    public override BoundNodeKind Kind => BoundNodeKind.NativeFunctionCallExpression;
    public NativeFunctionSymbol NativeFunction { get; } = nativeFunction;
    public BoundExpression[] Arguments { get; } = arguments;
}
