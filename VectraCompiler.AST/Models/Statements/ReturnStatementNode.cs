using VectraCompiler.AST.Models.Expressions;

namespace VectraCompiler.AST.Models.Statements;

public class ReturnStatementNode(IExpressionNode? value, SourceSpan span) : IStatementNode
{
    public IExpressionNode? Value { get; } = value;
    public SourceSpan Span { get; } = span;

    public ReturnStatementNode(SourceSpan span) : this(null, span) { }

    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStatement(this);
}