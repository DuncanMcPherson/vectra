namespace VectraCompiler.Core.Logging;

public interface ILogSink
{
    void Write(LogLevel level, CompilePhase phase, string message);
    void Setup(params string[] args);
    void Shutdown();
}