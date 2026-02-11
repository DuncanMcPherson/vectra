namespace VectraCompiler.Bind.Models.Symbols;

public sealed class PropertySymbol : VariableSymbol
{
    public bool HasGetter { get; }
    public bool HasSetter { get; }

    public PropertySymbol(string name, TypeSymbol type, bool hasGetter, bool hasSetter) : base(SymbolKind.Property,
        name, type)
    {
        HasGetter = hasGetter;
        HasSetter = hasSetter;
    }
}