using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public class UnaryExpressionNode(string op, IExpressionNode operand, SourceSpan span) : IExpressionNode
{
    public string Operator { get; } = op;
    public IExpressionNode Operand { get; } = operand;
    public SourceSpan Span { get; } = span;

    public string ToPrintable()
    {
        return $"{Operator}{Operand}";
    }
}