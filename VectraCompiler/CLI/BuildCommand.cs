using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using VectraCompiler.AST;
using VectraCompiler.AST.Lexing;
using VectraCompiler.Core;
using VectraCompiler.Core.ConsoleExtensions;
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
        var logLevel = settings.LogLevel.ToLogLevel();
        Logger.MinimumLevel = logLevel;
        var logsDir = Extensions.IsNullOrEmpty(settings.LogDir) ? "logs" : settings.LogDir;
        AnsiConsole.MarkupLine("[green]Building[/] [blue]{0}[/]", settings.Path);
        if (!settings.NoColor)
            Logger.AddSink(new SpectreConsoleSink());
        Logger.StartNewRun(logsDir);
        var res = await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn {Alignment = Justify.Left}, new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn(),
                new ElapsedTimeMsColumn()).StartAsync(async ctx =>
            {
                using var _ = Logger.BeginPhase(CompilePhase.Metadata, "Starting phase: Metadata");

                var packageDataResult =
                    await AstPhaseRunner.GetModulesToCompileAsync(ctx, cancellationToken, settings.Path);
                return packageDataResult;
            });
        if (!res.Ok) return 1;
        var modGraph = res.Value.Item1;
        var packageName = res.Value.Item2;
        return await CompileModule(packageName, modGraph.Order, cancellationToken);
    }

    private static async Task<int> CompileModule(string packageName, IReadOnlyList<ModuleMetadata> modules,
        CancellationToken ct)
    {
        return await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn {Alignment = Justify.Left}, new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn(),
                new ElapsedTimeMsColumn()).StartAsync(async ctx =>
            {
                using var _ = Logger.BeginPhase(CompilePhase.Parse, "Parsing modules");
                var package = new VectraAstPackage
                {
                    Name = packageName
                };
                foreach (var module in modules)
                {
                    var modParseTask = ctx.AddTask($"[cyan]{module.Name}[/]", maxValue: 1);
                    var files = SourceFileDiscoverer.Discover(module);
                    modParseTask.MaxValue = files.Count + 1;
                    modParseTask.Increment(1);
                    if (files.Count == 0)
                        return 1;
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
                        var parser = new Parser(tokens, file);
                        var parseResult = parser.Parse();
                        moduleAst.Files.Add(parseResult);
                        modParseTask.Increment(1);
                    }

                    package.AddModule(moduleAst);
                    modParseTask.StopTask();
                }

                return 0;
            });
    }
}