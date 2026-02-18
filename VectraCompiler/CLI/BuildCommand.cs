using Spectre.Console;
using Spectre.Console.Cli;
using VectraCompiler.Analysis;
using VectraCompiler.Lower;
using VectraCompiler.AST;
using VectraCompiler.Bind;
using VectraCompiler.Core.Logging;
using VectraCompiler.Package;
using Extensions = VectraCompiler.Core.Extensions;

namespace VectraCompiler.CLI;

internal sealed class BuildCommand : AsyncCommand<BuildSettings>
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
        var res = await MetadataRunner.RunPhaseAsync(settings.Path, cancellationToken);
        if (!res.Ok) return 1;
        var modGraph = res.Value.Item1;
        var packageName = res.Value.Item2;
        var packageAst = await AstPhaseRunner.CompileModules(packageName, modGraph.Order, cancellationToken);
        if (!packageAst.Ok) return 1;

        var package = packageAst.Value!;
        var bindResult = await BindPhaseRunner.RunInitialBindingAsync(package, cancellationToken);
        if (!bindResult.Ok)
            return 1;
        var analyzeResult = await AnalyzePhaseRunner.RunAsync(bindResult.Value!, cancellationToken);
        if (!analyzeResult.Ok)
            return 1;
        var lowerResult = await LowerPhaseRunner.RunAsync(analyzeResult.Value!, cancellationToken);
        if (!lowerResult.Ok)
            return 1;
        var groupedModules = Grouper.Run(
            lowerResult.Value!, res.Value.Item1.Order, package);
        return groupedModules.Ok ? 0 : 1;
    }

}