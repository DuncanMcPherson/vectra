namespace VectraCompiler.Bind.Models;

public sealed class ParameterSymbol : VariableSymbol
{
    public int Ordinal { get; }

    public ParameterSymbol(string name, TypeSymbol type, int ordinal)
        : base(SymbolKind.Parameter, name, type)
    {
        Ordinal = ordinal;
    }
}