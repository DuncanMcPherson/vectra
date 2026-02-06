namespace VectraCompiler.Bind.Models;

public class NamedTypeSymbol : TypeSymbol
{
    public NamedTypeKind TypeKind { get; }

    public NamedTypeSymbol(
        string name,
        NamedTypeKind kind)
        : base(name)
    {
        TypeKind = kind;
    }
}

// This is it for now. as we implement more features, we will add more type kinds
public enum NamedTypeKind
{
    Class
}