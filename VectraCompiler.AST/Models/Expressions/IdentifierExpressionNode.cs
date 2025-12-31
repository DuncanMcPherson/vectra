namespace VectraCompiler.AST.Models.Expressions;

public class IdentifierExpressionNode(string name, SourceSpan span) : IExpressionNode
{
    public string Name { get; } = name;
    public SourceSpan Span { get; } = span;

    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitIdentifierExpression(this);
}