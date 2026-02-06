namespace VectraCompiler.Bind.Models;

public abstract class TypeSymbol : Symbol
{
    protected TypeSymbol(string name) : base(SymbolKind.Type, name)
    {
    }

}