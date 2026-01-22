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
        var info = ErrorCatalog.GetOrDefault(errorCode);
        AnsiConsole.MarkupLine($"[bold white]{errorCode.ToCodeString()} - {info.Title}[/]\n");
        AnsiConsole.MarkupLine($"[white]{info.Description}[/]\n");
        if (info.HowToFix is not null)
        {
            AnsiConsole.MarkupLine("[bold white]How to fix:[/]");
            AnsiConsole.MarkupLine($"[white]{info.HowToFix}[/]");
        }

        if (info.Example is not null)
        {
            AnsiConsole.MarkupLine("[bold white]Example:[/]");
            AnsiConsole.MarkupLine($"[white]{info.Example}[/]");
        }
        
        return 0;
    }
}