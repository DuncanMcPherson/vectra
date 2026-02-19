using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Models;

public static class NativeFunctionRegistry
{
    private static readonly Dictionary<string, NativeFunctionSymbol> _functions = new()
    {
        ["Print"] = new("Print", 0, BuiltInTypeSymbol.Void, [BuiltInTypeSymbol.String]),
        ["PrintLine"] = new("PrintLine", 1, BuiltInTypeSymbol.Void, [BuiltInTypeSymbol.String]),
        ["Read"] = new("Read", 2, BuiltInTypeSymbol.String, []),
        ["ReadLine"] = new("ReadLine", 3, BuiltInTypeSymbol.String, []),
        ["ReadInt"] = new("ReadInt", 4, BuiltInTypeSymbol.Number, []),
    };
    
    public static bool TryGet(string name, out NativeFunctionSymbol symbol) => _functions.TryGetValue(name, out symbol!);
}