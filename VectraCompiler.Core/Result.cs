using VectraCompiler.Core.Errors;

namespace VectraCompiler.Core;

public record Result<T>(T? Value, DiagnosticBag Diagnostics)
{
    public bool Ok => Value is not null && !Diagnostics.HasErrors;
    
    public static Result<T> Pass(T value, DiagnosticBag bag) => new Result<T>(value, bag);
    public static Result<T> Fail(DiagnosticBag bag) => new Result<T>(default, bag);
}