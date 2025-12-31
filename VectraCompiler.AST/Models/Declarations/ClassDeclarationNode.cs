using VectraCompiler.AST.Models.Declarations.Interfaces;

namespace VectraCompiler.AST.Models.Declarations;

public class ClassDeclarationNode(string name, IList<IMemberNode> members, SourceSpan span) : ITypeDeclarationNode
{
    public string Name { get; } = name;
    public IList<IMemberNode> Members { get; } = members;
    public SourceSpan Span { get; } = span;

    public T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitClassDeclaration(this);

    public override string ToString()
    {
        return $"class {Name}: {Members.Count} members, [{string.Join(", ", Members)}]";
    }
}