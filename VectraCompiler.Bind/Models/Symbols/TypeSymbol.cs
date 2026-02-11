namespace VectraCompiler.Bind.Models.Symbols;

public abstract class TypeSymbol : Symbol
{
    protected TypeSymbol(string name) : base(SymbolKind.Type, name)
    {
    }

}