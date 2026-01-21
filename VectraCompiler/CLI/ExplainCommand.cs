using Spectre.Console;
using Spectre.Console.Cli;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.CLI;

public class ExplainCommand : Command<ExplainSettings>
{
    public override int Execute(CommandContext context, ExplainSettings settings, CancellationToken cancellationToken)
    {
        if (!settings.CodeString.TryParseCodeString(out var errorCode))
        {
            AnsiConsole.MarkupLine($"[red]Invalid error code format: {settings.CodeString}[/]");
            return 1;
        }
        // TODO: Load error explanations and print
        return 0;
    }
}