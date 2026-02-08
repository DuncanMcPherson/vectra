namespace VectraCompiler.Bind.Models.Symbols;

public class NamedTypeSymbol : TypeSymbol
{
    public NamedTypeKind TypeKind { get; }
    public string FullName { get; }

    public NamedTypeSymbol(
        string name,
        string fullName,
        NamedTypeKind kind)
        : base(name)
    {
        FullName = fullName;
        TypeKind = kind;
    }
}

// This is it for now. as we implement more features, we will add more type kinds
public enum NamedTypeKind
{
    Class
}