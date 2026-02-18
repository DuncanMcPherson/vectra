using Spectre.Console;
using VectraCompiler.Analysis.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Core;
using VectraCompiler.Core.ConsoleExtensions;
using VectraCompiler.Core.Logging;
using VectraCompiler.Lower.Models;
using VectraCompiler.Lower.Transformers;

namespace VectraCompiler.Lower;

public static class LowerPhaseRunner
{
    private sealed class DefaultLowerer : BoundTreeRewriter
    {
        // Currently just a pass-through, can be extended for specific lowering rules
    }

    public static Task<Result<LoweredResult>> RunAsync(
        AnalyzeResult analyzeResult,
        CancellationToken ct = default)
    {
        return AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn { Alignment = Justify.Left }, new ProgressBarColumn(),
                new PercentageColumn(), new SpinnerColumn(), new ElapsedTimeMsColumn())
            .StartAsync(ctx =>
            {
                using var _ = Logger.BeginPhase(CompilePhase.Lower, "Starting lowering");
                
                var bodies = analyzeResult.BindResult.BodiesByMember;
                var task = ctx.AddTask("Lowering method bodies", maxValue: bodies.Count);
                
                var loweredBodies = new Dictionary<CallableSymbol, BoundStatement>();
                var lowerer = new DefaultLowerer();

                foreach (var (symbol, body) in bodies)
                {
                    ct.ThrowIfCancellationRequested();

                    if (symbol is not CallableSymbol callable)
                    {
                        task.Increment(1);
                        continue;
                    }

                    Logger.LogTrace($"Lowering '{callable.Name}'");
                    var loweredBody = lowerer.RewriteStatement(body);
                    loweredBodies.Add(callable, loweredBody);
                    task.Increment(1);
                }

                task.StopTask();
                Logger.LogInfo("Lowering completed.");

                var result = new LoweredResult
                {
                    AnalyzeResult = analyzeResult,
                    LoweredBodies = loweredBodies
                };

                return Task.FromResult(Result<LoweredResult>.Pass(result, analyzeResult.Diagnostics));
            });
    }
}
