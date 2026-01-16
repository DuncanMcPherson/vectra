namespace VectraCompiler.AST.Models.Expressions;

public class IdentifierExpressionNode(string name, SourceSpan span) : AstNodeBase, IExpressionNode
{
    public string Name { get; } = name;
    public override SourceSpan Span { get; } = span;

    public override T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitIdentifierExpression(this);

    public override string ToPrintable()
    {
        return $"{Name}";
    }
}