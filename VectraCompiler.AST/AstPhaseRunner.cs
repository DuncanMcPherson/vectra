using Spectre.Console;
using VectraCompiler.AST.Lexing;
using VectraCompiler.Core;
using VectraCompiler.Core.ConsoleExtensions;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;
using VectraCompiler.Package;
using VectraCompiler.Package.Models;

namespace VectraCompiler.AST;

public static class AstPhaseRunner
{
    public static async Task<Result<VectraAstPackage>> CompileModules(string packageName,
        IReadOnlyList<ModuleMetadata> modules,
        CancellationToken ct)
    {
        return await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn { Alignment = Justify.Left }, new ProgressBarColumn(),
                new PercentageColumn(), new SpinnerColumn(),
                new ElapsedTimeMsColumn()).StartAsync(async ctx =>
            {
                using var _ = Logger.BeginPhase(CompilePhase.Parse, "Parsing modules");
                var package = new VectraAstPackage
                {
                    Name = packageName
                };
                var db = new DiagnosticBag();
                foreach (var module in modules)
                {
                    var modParseTask = ctx.AddTask($"[cyan]{module.Name}[/]", maxValue: 1);
                    var files = SourceFileDiscoverer.Discover(module);
                    modParseTask.MaxValue = files.Count + 1;
                    modParseTask.Increment(1);
                    if (files.Count == 0)
                    {
                        db.Add(new Diagnostic(ErrorCode.NoFilesFoundForModule, Severity.Error,
                            $"No files found for module {module.Name}"));
                        modParseTask.StopTask();
                        continue;
                    }

                    var moduleAst = new VectraAstModule
                    {
                        ModuleName = module.Name
                    };
                    foreach (var file in files)
                    {
                        ct.ThrowIfCancellationRequested();
                        Logger.LogTrace($"Compiling {file}...");
                        var lexer = new Lexer();
                        var sourceString = await File.ReadAllTextAsync(file, ct);
                        var tokens = lexer.ReadTokens(sourceString);
                        if (!tokens.Ok)
                        {
                            db.AddRange(tokens.Diagnostics.Items);
                            continue;
                        }

                        var parser = new Parser(tokens.Value!, file);
                        var parseResult = parser.Parse();
                        if (parseResult.HasErrors)
                        {
                            db.AddRange(parseResult.Diagnostics);
                        }
                        moduleAst.Files.Add(parseResult);
                        modParseTask.Increment(1);
                    }

                    package.AddModule(moduleAst);
                    modParseTask.StopTask();
                }

                var errorCount = db.Items.Count(x => x.Severity == Severity.Error);
                foreach (var error in db.Items)
                {
                    Logger.LogError($"{error.Code.ToCodeString()} - {error.Message} at {error.Span!.StartLine}:{error.Span!.StartColumn}");
                }
                Logger.LogInfo($"Parsing completed with {errorCount} error{(errorCount != 1 ? "s" : "")}.");
                return db.HasErrors ? Result<VectraAstPackage>.Fail(db) : Result<VectraAstPackage>.Pass(package, db);
            });
    }
}