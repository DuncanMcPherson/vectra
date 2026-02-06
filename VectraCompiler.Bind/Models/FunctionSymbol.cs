namespace VectraCompiler.Bind.Models;

public sealed class FunctionSymbol : Symbol
{
    public TypeSymbol ReturnType { get; }
    public IReadOnlyList<ParameterSymbol> Parameters { get; }

    public FunctionSymbol(string name, TypeSymbol returnType, IReadOnlyList<ParameterSymbol> parameters)
        : base(SymbolKind.Function, name)
    {
        ReturnType = returnType;
        Parameters = parameters;
    }
}