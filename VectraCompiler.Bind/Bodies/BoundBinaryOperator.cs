using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Bodies;

public sealed class BoundBinaryOperator(BoundBinaryOperatorKind kind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol resultType)
{
    public BoundBinaryOperatorKind Kind { get; } = kind;
    public TypeSymbol LeftType { get; } = leftType;
    public TypeSymbol RightType { get; } = rightType;
    public TypeSymbol ResultType { get; } = resultType;
}