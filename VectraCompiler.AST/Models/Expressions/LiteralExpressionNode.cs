namespace VectraCompiler.AST.Models.Expressions;

public class LiteralExpressionNode(object value, SourceSpan span) : IExpressionNode
{
    public object Value { get; } = value;
    public SourceSpan Span { get; } = span;
    
    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitLiteralExpression(this);
}