using VectraCompiler.Bind.Models;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Analysis.Models;

public sealed class AnalyzeResult
{
    public required BodyBindResult BindResult { get; init; }
    public required DiagnosticBag Diagnostics { get; init; }
}