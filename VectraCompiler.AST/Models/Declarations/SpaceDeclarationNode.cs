using VectraCompiler.AST.Models.Declarations.Interfaces;

namespace VectraCompiler.AST.Models.Declarations;

public class SpaceDeclarationNode
{
    public string Name { get; }
    public string QualifiedName => $"{Parent?.Name}{(Parent == null ? string.Empty : '.')}{Name}";
    public IList<ITypeDeclarationNode> Declarations { get; }
    public SourceSpan Span { get; }
    public IList<SpaceDeclarationNode> Subspaces { get; } = [];
    public SpaceDeclarationNode? Parent { get; private set; }

    public SpaceDeclarationNode(string name, IList<ITypeDeclarationNode> declarations, SourceSpan span,
        SpaceDeclarationNode? parent = null)
    {
        Name = name;
        Declarations = declarations;
        Span = span;
        Parent = parent;
        Parent?.Subspaces.Add(this);
    }

    public void AddTypes(IList<ITypeDeclarationNode> types)
    {
        Declarations.AddRange(types);
    }

    public void AddSubspace(SpaceDeclarationNode space)
    {
        space.SetParent(this);
        Subspaces.Add(space);
    }

    public void SetParent(SpaceDeclarationNode parent)
    {
        Parent ??= parent;
        parent?.Subspaces.Add(this);
    }

    public override string ToString()
    {
        return $"space {QualifiedName};\n\n{string.Join("\n\n", Declarations)}{(Declarations.Count > 0 ? $"\n\n{string.Join("\n\n", Subspaces)}" : $"{string.Join("\n\n", Subspaces)}")}";
    }
}