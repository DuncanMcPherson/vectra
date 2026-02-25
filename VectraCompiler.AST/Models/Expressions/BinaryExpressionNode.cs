using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public class BinaryExpressionNode(string op, IExpressionNode left, IExpressionNode right, SourceSpan span) : AstNodeBase, IExpressionNode
{
    public string Operator { get; } = op;
    public IExpressionNode Left { get; } = left;
    public IExpressionNode Right { get; } = right;
    public override SourceSpan Span { get; } = span;
    
    public override string ToPrintable()
    {
        return $"{Left} {Operator} {Right}";
    }
}