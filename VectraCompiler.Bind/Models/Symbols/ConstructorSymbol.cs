namespace VectraCompiler.Bind.Models.Symbols;

public sealed class ConstructorSymbol(NamedTypeSymbol containingType, IReadOnlyList<ParameterSymbol> parameters)
    : Symbol(SymbolKind.Constructor, containingType.Name)
{
    public NamedTypeSymbol ContainingType { get; } = containingType;
    public IReadOnlyList<ParameterSymbol> Parameters { get; } = parameters;

    public int Arity => Parameters.Count;
}