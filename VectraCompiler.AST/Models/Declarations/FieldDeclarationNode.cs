using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.AST.Models.Expressions;

namespace VectraCompiler.AST.Models.Declarations;

public class FieldDeclarationNode(string name, string type, IExpressionNode? initializer, SourceSpan span) : AstNodeBase, IMemberNode
{
    public override SourceSpan Span { get; } = span;
    public override T Visit<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFieldDeclaration(this);
    }

    public string Name { get; } = name;
    public string Type { get; } = type;
    public IExpressionNode? Initializer { get; } = initializer;

    public override string ToPrintable()
    {
        return $"{Type} {Name}{(initializer != null ? $" = {Initializer}" : "")};";
    }
}