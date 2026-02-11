using VectraCompiler.Core.Errors;

namespace VectraCompiler.Core.Logging;

public static class Logger
{
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;
    private static AsyncLocal<CompilePhase> CurrentCompilePhase { get; } = new()
    {
        Value = CompilePhase.Boot
    };
    public static CompilePhase CurrentPhase => CurrentCompilePhase.Value;
    private static readonly List<ILogSink> Sinks = [];
    public static void AddSink(ILogSink sink) => Sinks.Add(sink);

    public static IDisposable BeginPhase(CompilePhase phase, string? message = null)
    {
        var previous = CurrentCompilePhase.Value;
        CurrentCompilePhase.Value = phase;
        if (message is not null)
            LogInfo(CurrentPhase, message);
        return new PhaseScope(previous);
    }

    public static void LogTrace(CompilePhase phase, string message)
    {
        if (LogLevel.Trace < MinimumLevel) return;
        
        foreach (var sink in Sinks)
            sink.Write(LogLevel.Trace, phase, message);
    }

    public static void LogTrace(string message)
    {
        LogTrace(CurrentPhase, message);
    }

    public static void LogDiagnostic(Diagnostic diagnostic)
    {
        switch (diagnostic.Severity)
        {
            case Severity.Error:
                LogError($"{diagnostic.CodeString} {diagnostic.Message}");
                break;
            case Severity.Warning:
                LogWarning($"{diagnostic.CodeString} {diagnostic.Message}");
                break;
            case Severity.Info:
                LogInfo($"{diagnostic.CodeString} {diagnostic.Message}");
                break;
        }
    }

    public static void LogDebug(CompilePhase phase, string message)
    {
        if (LogLevel.Debug < MinimumLevel) return;
        foreach (var sink in Sinks)
            sink.Write(LogLevel.Debug, phase, message);
    }

    public static void LogDebug(string message)
    {
        LogDebug(CurrentPhase, message);
    }

    public static void LogInfo(CompilePhase phase, string message)
    {
        if (LogLevel.Info < MinimumLevel) return;
        foreach (var sink in Sinks)
            sink.Write(LogLevel.Info, phase, message);
    }

    public static void LogInfo(string message)
    {
        LogInfo(CurrentPhase, message);
    }

    public static void LogWarning(CompilePhase phase, string message)
    {
        if (LogLevel.Warning < MinimumLevel) return;
        foreach (var sink in Sinks)
            sink.Write(LogLevel.Warning, phase, message);
    }

    public static void LogWarning(string message)
    {
        LogWarning(CurrentPhase, message);
    }

    public static void LogError(CompilePhase phase, string message)
    {
        if (LogLevel.Error < MinimumLevel) return;
        foreach (var sink in Sinks)
            sink.Write(LogLevel.Error, phase, message);
    }

    public static void LogError(string message)
    {
        LogError(CurrentPhase, message);
    }

    public static void LogException(CompilePhase phase, Exception ex, string message)
    {
        if (LogLevel.Error < MinimumLevel) return;
        foreach (var sink in Sinks)
            sink.Write(LogLevel.Error, phase, $"{message}{Environment.NewLine}{ex.Message}");
    }

    public static void LogException(Exception ex, string message)
    {
        LogException(CurrentPhase, ex, message);
    }


    public static void StartNewRun(string logDir = "logs")
    {
        foreach (var sink in Sinks)
        {
            sink.Setup(logDir);
        }
    }

    public static void Shutdown()
    {
        foreach (var sink in Sinks)
        {
            sink.Shutdown();
        }
    }

    private sealed class PhaseScope(CompilePhase previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CurrentCompilePhase.Value = previous;
        }
    }
}

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error
}

public enum CompilePhase
{
    Boot,
    Metadata,
    Parse,
    Bind,
    Analyze,
    Lower,
    Emit
}

public static class LogLevelExtensions
{
    public static LogLevel ToLogLevel(this string logLevel)
    {
        return Enum.Parse<LogLevel>(logLevel);
    }
}