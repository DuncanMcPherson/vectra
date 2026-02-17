using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public class LiteralExpressionNode(object value, SourceSpan span) : AstNodeBase, IExpressionNode
{
    public object Value { get; } = value;
    public override SourceSpan Span { get; } = span;
    
    public override T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitLiteralExpression(this);
    public override string ToPrintable()
    {
        return $"{Value}";
    }
}