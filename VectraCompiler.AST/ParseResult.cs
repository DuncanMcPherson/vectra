using VectraCompiler.Core.Errors;

namespace VectraCompiler.AST;

public sealed record ParseResult(
    VectraAstFile Tree,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(d => d.Severity == Severity.Error);
}