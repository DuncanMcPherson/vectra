using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Analysis.Analyzers;

public sealed class UnreachableCodeAnalyzer : IAnalyzer
{
    public void Analyze(CallableSymbol _, BoundBlockStatement body, DiagnosticBag diagnostics)
        => CheckBlock(body, diagnostics);

    private static void CheckBlock(BoundBlockStatement body, DiagnosticBag diagnostics)
    {
        var terminated = false;

        foreach (var stmt in body.Statements)
        {
            if (terminated)
            {
                diagnostics.Warning(ErrorCode.UnreachableCode, "Unreachable code detected", stmt.Span);
                break;
            }
            
            if (stmt is BoundBlockStatement nested)
                CheckBlock(nested, diagnostics);
            if (IsTerminator(stmt))
                terminated = true;
        }
    }

    private static bool IsTerminator(BoundStatement stmt) => stmt switch
    {
        BoundReturnStatement => true,
        BoundBlockStatement b => BlockAlwaysTerminates(b),
        _ => false
    };

    private static bool BlockAlwaysTerminates(BoundBlockStatement block)
        => block.Statements.Any(IsTerminator);
}