namespace VectraCompiler.Bind.Models.Symbols;

public sealed class MethodSymbol : Symbol
{
    public TypeSymbol ReturnType { get; }
    public IReadOnlyList<ParameterSymbol> Parameters { get; }
    public int Arity => Parameters.Count;

    public MethodSymbol(string name, TypeSymbol returnType, IReadOnlyList<ParameterSymbol> parameters)
        : base(SymbolKind.Function, name)
    {
        ReturnType = returnType;
        Parameters = parameters.ToArray();
    }
}