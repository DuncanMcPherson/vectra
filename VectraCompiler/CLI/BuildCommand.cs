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
        var res = await MetadataRunner.RunPhaseAsync(settings.Path, cancellationToken);
        if (!res.Ok) return 1;
        var modGraph = res.Value.Item1;
        var packageName = res.Value.Item2;
        var packageAst = await AstPhaseRunner.CompileModules(packageName, modGraph.Order, cancellationToken);
        return packageAst.Ok ? 0 : 1;
    }

}