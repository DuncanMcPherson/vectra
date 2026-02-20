using VectraCompiler.Bind.Bodies.Expressions;

namespace VectraCompiler.Bind.Models.Symbols;

public sealed class FieldSymbol(string name, TypeSymbol type, NamedTypeSymbol containingType) : VariableSymbol(SymbolKind.Field, name, type)
{
    public BoundExpression? Initializer { get; set; }
    public NamedTypeSymbol ContainingType { get; } = containingType;
}