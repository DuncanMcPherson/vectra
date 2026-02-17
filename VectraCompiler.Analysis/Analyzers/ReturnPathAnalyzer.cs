using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Analysis.Analyzers;

public sealed class ReturnPathAnalyzer : IAnalyzer
{
    public void Analyze(CallableSymbol callable, BoundBlockStatement body, DiagnosticBag diagnostics)
    {
        if (callable is ConstructorSymbol || ReferenceEquals(callable.ReturnType, BuiltInTypeSymbol.Void))
            return;

        if (!BlockAlwaysReturns(body))
        {
            diagnostics.Error(
                ErrorCode.MissingReturnPath,
                $"'{callable.Name}': not all code paths return a value.", body.Span);
        }
    }

    private static bool BlockAlwaysReturns(BoundBlockStatement block)
    {
        return block.Statements.Any(StatementAlwaysReturns);
    }

    private static bool StatementAlwaysReturns(BoundStatement stmt) => stmt switch
    {
        BoundReturnStatement => true,
        BoundBlockStatement block => BlockAlwaysReturns(block),
        _ => false
    };
}