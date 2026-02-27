using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public class UnaryExpressionNode(string op, IExpressionNode operand, SourceSpan span) : AstNodeBase, IExpressionNode
{
    public string Operator { get; } = op;
    public IExpressionNode Operand { get; } = operand;
    public override SourceSpan Span { get; } = span;

    public override string ToPrintable()
    {
        return $"{Operator}{Operand}";
    }
}