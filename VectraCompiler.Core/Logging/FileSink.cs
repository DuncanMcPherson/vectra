namespace VectraCompiler.Core.Logging;

public sealed class FileSink : ILogSink
{
    private StreamWriter? _writer;
    public string? LogPath { get; private set; }

    public void Setup(params string[]? args)
    {
        string logDir;
        if (args is { Length: > 0 })
        {
            logDir = args[0];
        }
        else
        {
            logDir = "logs";
        }
        Directory.CreateDirectory(logDir);

        var ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        LogPath = Path.Combine(logDir, $"vectra-compile-{ts}.log");
        
        _writer = new StreamWriter(File.Open(LogPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
        
        Write(LogLevel.Info, CompilePhase.Boot, $"Log started: {LogPath}");
    }
    
    public void Write(LogLevel level, CompilePhase phase, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{phase}] {message}";
        _writer?.WriteLine(line);
    }
    
    public void Shutdown()
    {
        Write(LogLevel.Info, CompilePhase.Boot, "Log ended");
        _writer?.Dispose();
        _writer = null;
    }
}