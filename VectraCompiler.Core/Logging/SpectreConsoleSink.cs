using Spectre.Console;

namespace VectraCompiler.Core.Logging;

public sealed class SpectreConsoleSink : ILogSink
{
    public void Write(LogLevel level, CompilePhase phase, string message)
    {
        var color = level switch
        {
            LogLevel.Error => "red",
            LogLevel.Warning => "orange",
            LogLevel.Info => "blue",
            LogLevel.Debug => "green",
            LogLevel.Trace => "dim",
            _ => "white"
        };

        var line = $"[{color}][[{phase}]] {message.EscapeMarkup()}[/]";
        AnsiConsole.MarkupLine(line);
    }

    public void Setup(params string[] args)
    {
        // Intentionally a no-op
    }

    public void Shutdown()
    {
        Write(LogLevel.Info, CompilePhase.Boot, "Shutting down...");
    }
}