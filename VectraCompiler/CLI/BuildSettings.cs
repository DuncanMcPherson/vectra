using Spectre.Console.Cli;

namespace VectraCompiler.CLI;

public sealed class BuildSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    public string Path { get; init; } = string.Empty;
    
    [CommandOption("--log-level <LEVEL>")]
    public string LogLevel { get; init; } = "Info";
    
    [CommandOption("--log-dir <DIR>")]
    public string LogDir { get; init; } = string.Empty;
    
    [CommandOption("--no-color")]
    public bool NoColor { get; init; } = false;
}