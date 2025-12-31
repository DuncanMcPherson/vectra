using VectraCompiler.AST.Models.Expressions;

namespace VectraCompiler.AST.Models.Statements;

public class ExpressionStatementNode(IExpressionNode expression, SourceSpan span) : IStatementNode
{
    public IExpressionNode Expression { get; } = expression;
    public SourceSpan Span { get; } = span;
    
    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitExpressionStatement(this);
}