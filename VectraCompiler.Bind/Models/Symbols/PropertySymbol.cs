namespace VectraCompiler.Bind.Models.Symbols;

public sealed class PropertySymbol(
    string name,
    TypeSymbol type,
    NamedTypeSymbol containingType,
    bool hasGetter,
    bool hasSetter)
    : VariableSymbol(SymbolKind.Property,
        name, type)
{
    public bool HasGetter { get; } = hasGetter;
    public bool HasSetter { get; } = hasSetter;
    public NamedTypeSymbol ContainingType { get; } = containingType;
}