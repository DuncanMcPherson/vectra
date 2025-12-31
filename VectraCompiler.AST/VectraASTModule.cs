using VectraCompiler.AST.Models.Declarations;

namespace VectraCompiler.AST;

public class VectraAstModule
{
    public required string Name { get; init; }
    public required bool IsExecutable { get; init; }
    public required SpaceDeclarationNode Space { get; init; }

    public void InsertSpace(SpaceDeclarationNode space)
    {
        var newParts = space.QualifiedName.Split('.');
        var parentSpace = EnsureSpacePath(newParts);
        parentSpace.AddSubspace(space);
    }

    private SpaceDeclarationNode EnsureSpacePath(IReadOnlyList<string> newParts)
    {
        var current = Space;

        if (current.Name == newParts[0])
        {
            for (var i = 1; i < newParts.Count; i++)
            {
                var currentNamePart = newParts[i];
                var selected = current.Subspaces.FirstOrDefault(x => x.Name == currentNamePart);
                if (selected == null)
                {
                    return current;
                }
            }
        }

        return current;
    }
}