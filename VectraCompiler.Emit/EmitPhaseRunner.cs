using Spectre.Console;
using VectraCompiler.Core;
using VectraCompiler.Core.ConsoleExtensions;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;
using VectraCompiler.Emit.Models;
using VectraCompiler.Emit.Services;
using VectraCompiler.Lower.Models;

namespace VectraCompiler.Emit;

public static class EmitPhaseRunner
{
    public static Task<Result<EmitResult>> RunAsync(
        List<LoweredModule> modules,
        CancellationToken ct = default)
    {
        return AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn { Alignment = Justify.Left }, new ProgressBarColumn(),
                new PercentageColumn(), new SpinnerColumn(), new ElapsedTimeMsColumn())
            .StartAsync(async ctx =>
            {
                using var _ = Logger.BeginPhase(CompilePhase.Emit, "Starting emit");
                var db = new DiagnosticBag();
                var task = ctx.AddTask("Emitting modules", maxValue: modules.Count);

                var emittedPaths = new List<string>();

                foreach (var module in modules)
                {
                    ct.ThrowIfCancellationRequested();
                    Logger.LogTrace($"Emitting module '{module.ModuleName}' ({module.ModuleType})");

                    try
                    {
                        var outputDir = GetOutputDir(module);
                        var emitter = new ModuleEmitter(module);
                        await emitter.EmitAsync(outputDir, ct);

                        Logger.LogInfo($"Emitted '{module.ModuleName}' to '{outputDir}'");
                        emittedPaths.Add(outputDir);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex, $"Failed to emit module '{module.ModuleName}'");
                        db.Error(ErrorCode.InternalError, $"Emit failed for '{module.ModuleName}': {ex.Message}");
                    }

                    task.Increment(1);
                }

                task.StopTask();

                var errorCount = db.Items.Count(x => x.Severity == Severity.Error);
                Logger.LogInfo($"Emit completed with {errorCount} error(s)");

                var result = new EmitResult
                {
                    EmittedModulePaths = emittedPaths,
                    Modules = modules
                };

                return db.HasErrors
                    ? Result<EmitResult>.Fail(db)
                    : Result<EmitResult>.Pass(result, db);
            });
    }

    private static string GetOutputDir(LoweredModule module)
    {
        return Path.Combine(module.ModuleRoot, "bin", "Debug");
    }
}