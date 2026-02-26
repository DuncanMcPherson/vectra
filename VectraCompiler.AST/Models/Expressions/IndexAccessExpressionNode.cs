using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public sealed class IndexAccessExpressionNode(
    IExpressionNode target,
    IExpressionNode index,
    SourceSpan span) : AstNodeBase, IExpressionNode
{
    public IExpressionNode Target { get; } = target;
    public IExpressionNode Index { get; } = index;
    public override SourceSpan Span { get; } = span;

    public override string ToPrintable()
    {
        return $"{Target}[{Index}]";
    }
}