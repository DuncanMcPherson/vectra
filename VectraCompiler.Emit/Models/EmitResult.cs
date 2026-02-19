using VectraCompiler.Lower.Models;

namespace VectraCompiler.Emit.Models;

public class EmitResult
{
    public required IReadOnlyList<string> EmittedModulePaths { get; init; }
    public required IReadOnlyList<LoweredModule> Modules { get; init; }
}