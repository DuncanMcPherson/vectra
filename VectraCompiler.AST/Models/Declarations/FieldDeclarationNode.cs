using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.AST.Models.Expressions;

namespace VectraCompiler.AST.Models.Declarations;

public class FieldDeclarationNode(string name, string type, IExpressionNode? initializer, SourceSpan span) : IMemberNode
{
    public SourceSpan Span { get; } = span;
    public T Visit<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFieldDeclaration(this);
    }

    public string Name { get; } = name;
    public string Type { get; } = type;
    public IExpressionNode? Initializer { get; } = initializer;
}