using VectraCompiler.AST.Models.Declarations.Interfaces;

namespace VectraCompiler.AST.Models.Declarations;

public class PropertyDeclarationNode(string name, string type, SourceSpan span, bool hasGetter, bool hasSetter) : IMemberNode
{
    public SourceSpan Span { get; } = span;
    public T Visit<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitPropertyDeclaration(this);
    }

    public string Name { get; } = name;
    public bool HasGetter { get; } = hasGetter;
    public bool HasSetter { get; } = hasSetter;
    public string Type { get; } = type;
    // TODO: add a way to have getter and setter method bodies
}