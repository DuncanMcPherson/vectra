namespace VectraCompiler.Bind.Models.Symbols;

public class CallableSymbol(SymbolKind kind, string name, TypeSymbol returnType, IReadOnlyList<ParameterSymbol> parameters)
: Symbol(kind, name)
{
    public TypeSymbol ReturnType { get; } = returnType;
    public IReadOnlyList<ParameterSymbol> Parameters { get; } = parameters;
    public int Arity => Parameters.Count;
}