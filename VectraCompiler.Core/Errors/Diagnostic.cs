using VectraCompiler.Core.Logging;

namespace VectraCompiler.Core.Errors;

public enum Severity
{
    Info,
    Warning,
    Error
}

public record Diagnostic(
    ErrorCode Code,
    Severity Severity,
    string Message,
    SourceSpan? Span = null,
    string? File = null)
{
    public string CodeString => Code.ToCodeString();
}

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _items = [];
    public IReadOnlyList<Diagnostic> Items => _items;

    public bool HasErrors => _items.Any(d => d.Severity == Severity.Error);

    public DiagnosticBag Add(Diagnostic diagnostic)
    {
        Logger.LogDiagnostic(diagnostic);
        _items.Add(diagnostic);
        return this;
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            _items.Add(diagnostic);
    }

    public DiagnosticBag Error(ErrorCode code, string message, SourceSpan? span = null, string? file = null) =>
        Add(new(code, Severity.Error, message, span, file));
    public DiagnosticBag Warning(ErrorCode code, string message, SourceSpan? span = null, string? file = null) =>
        Add(new(code, Severity.Warning, message, span, file));
    public DiagnosticBag Warning(ErrorCode code, string message, SourceSpan span) => Warning(code, message, span, null);
}