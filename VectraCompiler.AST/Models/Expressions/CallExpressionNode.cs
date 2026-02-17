using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public class CallExpressionNode(
    IExpressionNode target,
    IList<IExpressionNode> arguments,
    SourceSpan span) : AstNodeBase, IExpressionNode
{
    public IExpressionNode Target { get; } = target;
    public IList<IExpressionNode> Arguments { get; } = arguments;
    public override SourceSpan Span { get; } = span;
    
    public override T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitCallExpression(this);
    public override string ToPrintable()
    {
        return $"{Target}({string.Join(", ", Arguments)})";
    }
}