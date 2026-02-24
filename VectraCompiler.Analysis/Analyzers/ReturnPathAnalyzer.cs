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
        BoundTryStatement tryStatement => AttemptBlockAlwaysReturns(tryStatement),
        _ => false
    };
    
    private static bool AttemptBlockAlwaysReturns(BoundTryStatement tryStatement)
    {
        var attemptReturns = BlockAlwaysReturns(tryStatement.TryBlock);
        bool? recoverReturns;
        if (tryStatement.CatchClause is { } catchClause)
            recoverReturns = BlockAlwaysReturns(catchClause.Body);
        else
            recoverReturns = null;
        return attemptReturns && (recoverReturns is not null && recoverReturns.Value || recoverReturns is null);
    }
}