namespace VectraCompiler.Bind.Models.Symbols;

public sealed class ArrayTypeSymbol(TypeSymbol elementType) : TypeSymbol($"{elementType.Name}[]")
{
    public TypeSymbol ElementType { get; } = elementType;
}