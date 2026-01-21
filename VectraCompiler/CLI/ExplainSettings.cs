using Spectre.Console.Cli;

namespace VectraCompiler.CLI;

public class ExplainSettings : CommandSettings
{
    [CommandArgument(0, "<ErrorCode>")] public string CodeString { get; init; } = string.Empty;
}