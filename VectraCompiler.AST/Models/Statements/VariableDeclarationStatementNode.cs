using VectraCompiler.AST.Models.Expressions;

namespace VectraCompiler.AST.Models.Statements;

public class VariableDeclarationStatementNode(string name, string? explicitType, IExpressionNode? initializer, SourceSpan span) : IStatementNode
{
    public SourceSpan Span { get; } = span;
    public string Name { get; } = name;
    public string? ExplicitType { get; } = explicitType;
    public IExpressionNode? Initializer { get; } = initializer;

    public VariableDeclarationStatementNode(string name, string explicitType, SourceSpan span) : this(name, explicitType, null, span) { }
    public VariableDeclarationStatementNode(string name, IExpressionNode initializer, SourceSpan span) : this(name, null, initializer, span) { }

    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitVariableDeclarationStatement(this);
}