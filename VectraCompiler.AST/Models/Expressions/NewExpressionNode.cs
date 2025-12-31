namespace VectraCompiler.AST.Models.Expressions;

public class NewExpressionNode(string typeName, IList<IExpressionNode> arguments, SourceSpan span) : IExpressionNode
{
    public string TypeName { get; } = typeName;
    public IList<IExpressionNode> Arguments { get; } = arguments;
    public SourceSpan Span { get; } = span;

    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitNewExpression(this);
}