using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Declarations;

public class PropertyDeclarationNode(string name, string type, SourceSpan span, bool hasGetter, bool hasSetter) : AstNodeBase, IMemberNode
{
    public override SourceSpan Span { get; } = span;

    public string Name { get; } = name;
    public bool HasGetter { get; } = hasGetter;
    public bool HasSetter { get; } = hasSetter;
    public string Type { get; } = type;
    // TODO: add a way to have getter and setter method bodies

    public override string ToPrintable()
    {
        return $"{Type} {Name} {{ {(HasGetter ? "get; " : string.Empty)}{(HasSetter ? "set; " : string.Empty)}}}";
    }
}