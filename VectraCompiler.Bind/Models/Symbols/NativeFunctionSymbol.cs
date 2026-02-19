namespace VectraCompiler.Bind.Models.Symbols;

public class NativeFunctionSymbol(string name, int nativeIndex, TypeSymbol returnType, IReadOnlyList<TypeSymbol> parameterTypes) 
    : Symbol(SymbolKind.NativeFunction, name)
{
    public int NativeIndex { get; } = nativeIndex;
    public TypeSymbol ReturnType { get; } = returnType;
    public IReadOnlyList<TypeSymbol> ParameterTypes { get; } = parameterTypes;
}