namespace VectraCompiler.Bind.Models;

public sealed class LocalSymbol : VariableSymbol
{
    public LocalSymbol(string name, TypeSymbol type) : base(SymbolKind.Local, name, type)
    {
    }
}