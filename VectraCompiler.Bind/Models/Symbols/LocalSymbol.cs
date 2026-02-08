namespace VectraCompiler.Bind.Models.Symbols;

public sealed class LocalSymbol : VariableSymbol
{
    public LocalSymbol(string name, TypeSymbol type) : base(SymbolKind.Local, name, type)
    {
    }
}