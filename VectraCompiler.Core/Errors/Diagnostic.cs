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
    string? File = null,
    int? Line = null,
    int? Column = null)
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
        _items.Add(diagnostic);
        return this;
    }

    public DiagnosticBag Error(ErrorCode code, string message, string? file = null, int? line = null, int? column = null) =>
        Add(new(code, Severity.Error, message, file, line, column));
    public DiagnosticBag Warning(ErrorCode code, string message, string? file = null, int? line = null, int? column = null) =>
        Add(new(code, Severity.Warning, message, file, line, column));
}