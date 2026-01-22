using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using VectraCompiler.AST;
using VectraCompiler.AST.Lexing;
using VectraCompiler.Core;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;
using VectraCompiler.Package;
using VectraCompiler.Package.Models;
using Extensions = VectraCompiler.Core.Extensions;

namespace VectraCompiler.CLI;

[UsedImplicitly]
public sealed class BuildCommand : AsyncCommand<BuildSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, BuildSettings settings,
        CancellationToken cancellationToken)
    {
        SortResult modGraph = null!;
        var res = await AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn(),
                new ElapsedTimeColumn()).StartAsync(async ctx =>
            {
                var db = new DiagnosticBag();
                var pkgTask = ctx.AddTask("[cyan]Find & parse package[/]", maxValue: 4);
                var logLevel = settings.LogLevel.ToLogLevel();
                Logger.MinimumLevel = logLevel;
                var logsDir = Extensions.IsNullOrEmpty(settings.LogDir) ? "logs" : settings.LogDir;
                AnsiConsole.MarkupLine("[green]Building[/] [blue]{0}[/]", settings.Path);
                if (!settings.NoColor)
                    Logger.AddSink(new SpectreConsoleSink());
                Logger.StartNewRun(logsDir);
                using var _ = Logger.BeginPhase(CompilePhase.Metadata, "Starting phase: Metadata");

                var packageDataResult = await PackageReader.Read(pkgTask, settings.Path, cancellationToken);
                pkgTask.Increment(1);
                pkgTask.StopTask();
                if (!packageDataResult.Ok)
                {
                    foreach (var item in packageDataResult.Diagnostics.Items)
                    {
                        Logger.LogError(item.Message);
                    }

                    return new Result<int>(1, db);
                }

                var moduleMetadataTasks = packageDataResult.Value!.Modules
                    .Select(m => ctx.AddTask($"[cyan]{m.Name} metadata[/]", maxValue: 3)).ToList();

                List<ModuleMetadata> moduleMetadatas = new();
                var foundError = false;
                for (var i = 0; i < packageDataResult.Value!.Modules.Count; i++)
                {
                    var modTask = moduleMetadataTasks[i];
                    var modLocation = packageDataResult.Value!.Modules[i];
                    var modMetadata = await ModuleReader.Read(modTask, modLocation, cancellationToken);
                    if (!modMetadata.Ok)
                    {
                        foundError = true;
                        foreach (var item in modMetadata.Diagnostics.Items)
                        {
                            Logger.LogError(item.Message);
                        }
                    }
                    else
                    {
                        moduleMetadatas.Add(modMetadata.Value!);
                    }

                    modTask.Increment(1);
                    modTask.StopTask();
                }

                if (foundError) return new Result<int>(1, db);

                var graphTask = ctx.AddTask("[cyan]Build dependency graph[/]", maxValue: 4);

                var modGraphRes = DependencyGraphBuilder.TopoSort(moduleMetadatas, graphTask);
                graphTask.Increment(1);
                if (!modGraphRes.Ok || modGraphRes.Value!.HasCycle)
                {
                    if (modGraphRes.Value != null)
                    {
                        Logger.LogError(
                            $"Circular dependency detected: {string.Join(", ", modGraphRes.Value!.CycleNodes)}");
                    }
                    else
                    {
                        foreach (var d in modGraphRes.Diagnostics.Items)
                        {
                            Logger.LogError(d.Message);
                        }
                    }

                    return new Result<int>(1, db);
                }

                modGraph = modGraphRes.Value;
                return new Result<int>(0, db);
            });
        if (!res.Ok) return res.Value;
        return await CompileModule(modGraph.Order, cancellationToken);
    }

    private static async Task<int> CompileModule(IReadOnlyList<ModuleMetadata> modules, CancellationToken ct)
    {
        return await AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn(),
                new ElapsedTimeColumn()).StartAsync(async ctx =>
            {
                using var _ = Logger.BeginPhase(CompilePhase.Parse, "Parsing modules");
                foreach (var module in modules)
                {
                    var modParseTask = ctx.AddTask($"[cyan]{module.Name}[/]", maxValue: 1);
                    var files = SourceFileDiscoverer.Discover(module);
                    modParseTask.MaxValue = files.Count + 1;
                    modParseTask.Increment(1);
                    if (files.Count == 0)
                        return 1;
                    Logger.LogTrace($"Compiling {files[0]}...");
                    var sourceString = await File.ReadAllTextAsync(files[0].Trim(), ct);
                    var lexer = new Lexer();
                    var tokens = lexer.ReadTokens(sourceString);
                    var parser = new Parser(tokens, module);
                    var moduleAst = parser.Parse();
                    LogErrorsAndWarnings(files[0], parser);
                    modParseTask.Increment(1);
                    for (var i = 1; i < files.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var file = files[i];
                        Logger.LogTrace($"Compiling {file}...");
                        sourceString = await File.ReadAllTextAsync(file, ct);
                        tokens = lexer.ReadTokens(sourceString);
                        parser = new Parser(tokens, module);
                        var fileAst = parser.Parse();
                        LogErrorsAndWarnings(file, parser);
                        moduleAst.InsertSpace(fileAst.Space);
                        modParseTask.Increment(1);
                    }

                    modParseTask.StopTask();
                }

                return 0;
            });
    }

    private static void LogErrorsAndWarnings(string fileName, Parser parser)
    {
        if (parser.Diagnostics.Count == 0)
            return;
        Logger.LogWarning($"Found {parser.Diagnostics.Count} issues in {fileName}:");
        foreach (var diagnostic in parser.Diagnostics)
        {
            Logger.LogError($" - {diagnostic.Message} (at {diagnostic.Line}:{diagnostic.Column})");
        }
    }
}