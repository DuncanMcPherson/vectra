using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using VectraCompiler.AST;
using VectraCompiler.AST.Lexing;
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
        using var _ = Logger.BeginPhase(CompilePhase.Metadata, "Starting phase: Metadata");

        var packageData = await PackageReader.Read(settings.Path, cancellationToken);
        var moduleMetadatas = packageData.Modules.Select(ModuleReader.Read).ToList();
        using var __ = Logger.BeginPhase(CompilePhase.Parse, "Parsing modules");
        return await CompileModule(moduleMetadatas, cancellationToken);
    }

    private static async Task<int> CompileModule(List<ModuleMetadata> modules, CancellationToken ct)
    {
        foreach (var module in modules)
        {
            var files = SourceFileDiscoverer.Discover(module);
            if (files.Count == 0)
                continue;
            Logger.LogTrace($"Compiling {files[0]}...");
            var sourceString = await File.ReadAllTextAsync(files[0].Trim(),  ct);
            var lexer = new Lexer();
            var tokens = lexer.ReadTokens(sourceString);
            var parser = new Parser(tokens, module);
            var moduleAst = parser.Parse();
            for (var i = 1; i < files.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var file = files[i];
                sourceString = await File.ReadAllTextAsync(file, ct);
                tokens = lexer.ReadTokens(sourceString);
                parser = new Parser(tokens, module);
                var fileAst = parser.Parse();
                moduleAst.InsertSpace(fileAst.Space);
            }

            Logger.LogTrace(moduleAst.Space.ToString());
        }

        return 0;
    }
}