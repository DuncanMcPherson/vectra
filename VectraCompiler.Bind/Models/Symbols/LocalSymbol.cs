namespace VectraCompiler.Bind.Models.Symbols;

public sealed class LocalSymbol : VariableSymbol
{
    public int SlotIndex { get; set; } = -1;

    public LocalSymbol(string name, TypeSymbol type) : base(SymbolKind.Local, name, type)
    {
    }
}