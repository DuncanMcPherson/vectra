using Spectre.Console;
using VectraCompiler.Analysis.Analyzers;
using VectraCompiler.Analysis.Models;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;
using VectraCompiler.Core.ConsoleExtensions;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;

namespace VectraCompiler.Analysis;

public class AnalyzePhaseRunner
{
    private static readonly IReadOnlyList<IAnalyzer> Analyzers =
    [
        new ReturnPathAnalyzer(),
        new UninitializedVariableAnalyzer(),
        new UnreachableCodeAnalyzer(),
        new UnusedVariableAnalyzer(),
        new UnusedParameterAnalyzer()
    ];

    public static Task<Result<AnalyzeResult>> RunAsync(
        BodyBindResult bindResult,
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
                using var _ = Logger.BeginPhase(CompilePhase.Analyze, "Starting analysis");
                var db = new DiagnosticBag();
                var bodies = bindResult.BodiesByMember;
                var task = ctx.AddTask("Analyze method bodies", maxValue: bodies.Count * Analyzers.Count);

                foreach (var (symbol, body) in bodies)
                {
                    ct.ThrowIfCancellationRequested();

                    if (symbol is not CallableSymbol callable)
                    {
                        task.Increment(1 * Analyzers.Count);
                        continue;
                    }
                    
                    Logger.LogTrace($"Analyzing '{callable.Name}'");
                    foreach (var analyzer in Analyzers)
                    {
                        analyzer.Analyze(callable, body, db);
                        task.Increment(1);
                    }
                }
                
                task.StopTask();

                var errorCount = db.Items.Count(x => x.Severity == Severity.Error);
                var warnCount = db.Items.Count(x => x.Severity == Severity.Warning);
                
                Logger.LogInfo($"Analysis completed with {errorCount} error(s) and {warnCount} warning(s).");

                var analyzeResult = new AnalyzeResult
                {
                    BindResult = bindResult,
                    Diagnostics = db
                };

                return Task.FromResult(db.HasErrors
                    ? Result<AnalyzeResult>.Fail(db)
                    : Result<AnalyzeResult>.Pass(analyzeResult, db));
            });
    }
}