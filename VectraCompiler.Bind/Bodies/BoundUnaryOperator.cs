using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Bodies;

public enum BoundUnaryOperatorKind { Negate, LogicalNot }

public class BoundUnaryOperator(BoundUnaryOperatorKind kind, TypeSymbol operandType, TypeSymbol resultType)
{
    public BoundUnaryOperatorKind Kind { get; } = kind;
    public TypeSymbol OperandType { get; } = operandType;
    public TypeSymbol ResultType { get; } = resultType;

    private static readonly BoundUnaryOperator[] _operators =
    [
        new(BoundUnaryOperatorKind.Negate,     BuiltInTypeSymbol.Number, BuiltInTypeSymbol.Number),
        new(BoundUnaryOperatorKind.LogicalNot, BuiltInTypeSymbol.Bool,   BuiltInTypeSymbol.Bool),
    ];

    public static BoundUnaryOperator? Bind(string op, TypeSymbol operandType) =>
        _operators.FirstOrDefault(o =>
            o.OperandType == operandType &&
            op == o.Kind switch
            {
                BoundUnaryOperatorKind.Negate     => "-",
                BoundUnaryOperatorKind.LogicalNot => "!",
                _ => null
            });
}