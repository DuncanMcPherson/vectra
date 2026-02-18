using VectraCompiler.Analysis.Models;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Bind.Bodies.Statements;

namespace VectraCompiler.Lower.Models;

public sealed class LoweredResult
{
    public required AnalyzeResult AnalyzeResult { get; init; }
    public required IReadOnlyDictionary<CallableSymbol, BoundStatement> LoweredBodies { get; init; }
}
