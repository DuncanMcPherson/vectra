namespace VectraCompiler.Bind.Models.Symbols;

public sealed class ConstructorSymbol(NamedTypeSymbol containingType, IReadOnlyList<ParameterSymbol> parameters)
    : CallableSymbol(SymbolKind.Constructor, containingType.Name, containingType, parameters, containingType);