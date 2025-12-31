namespace VectraCompiler.AST.Models.Expressions;

public class BinaryExpressionNode(string op, IExpressionNode left, IExpressionNode right, SourceSpan span) : IExpressionNode
{
    public string Operator { get; } = op;
    public IExpressionNode Left { get; } = left;
    public IExpressionNode Right { get; } = right;
    public SourceSpan Span { get; } = span;
    
    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
}