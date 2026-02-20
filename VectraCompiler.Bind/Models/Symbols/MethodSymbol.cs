namespace VectraCompiler.Bind.Models.Symbols;

public sealed class MethodSymbol(string name, TypeSymbol returnType, IReadOnlyList<ParameterSymbol> parameters, NamedTypeSymbol containingType)
    : CallableSymbol(SymbolKind.Function, name, returnType, parameters, containingType);