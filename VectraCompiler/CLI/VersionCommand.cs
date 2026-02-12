using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace VectraCompiler.CLI;

public sealed class VersionCommand : Command<VersionCommand.Settings>
{
    public sealed class Settings : CommandSettings;

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
        AnsiConsole.MarkupLine($"[green]Vectra Compiler[/] {version}");
        return 0;
    }
}