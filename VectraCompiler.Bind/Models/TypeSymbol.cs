namespace VectraCompiler.Bind.Models;

public class TypeSymbol : Symbol
{
    protected TypeSymbol(string name) : base(SymbolKind.Type, name)
    {
    }

}