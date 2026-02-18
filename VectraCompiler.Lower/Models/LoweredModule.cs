using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Package.Models;

namespace VectraCompiler.Lower.Models;

public sealed class LoweredModule
{
    public required string ModuleName { get; init; }
    public required ModuleType ModuleType { get; init; }
    public required IReadOnlyList<NamedTypeSymbol> Types { get; init; }
    public required IReadOnlyDictionary<CallableSymbol, BoundStatement> LoweredBodies { get; init; }
}