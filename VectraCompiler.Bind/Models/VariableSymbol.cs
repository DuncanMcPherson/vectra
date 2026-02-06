namespace VectraCompiler.Bind.Models;

public abstract class VariableSymbol : Symbol
{
    public TypeSymbol Type { get; }

    protected VariableSymbol(SymbolKind kind, string name, TypeSymbol type) : base(kind, name)
    {
        Type = type;
    }
}