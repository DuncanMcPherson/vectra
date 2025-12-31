namespace VectraCompiler.AST.Models.Expressions;

public class CallExpressionNode(
    IExpressionNode target,
    string methodName,
    IList<IExpressionNode> arguments,
    SourceSpan span) : IExpressionNode
{
    public IExpressionNode Target { get; } = target;
    public string MethodName { get; } = methodName;
    public IList<IExpressionNode> Arguments { get; } = arguments;
    public SourceSpan Span { get; } = span;
    
    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitCallExpression(this);
}