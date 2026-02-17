using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Analysis.Tests.Utilities;

public abstract class AnalyzerTestBase
{
    protected static readonly NamedTypeSymbol TestType = new("TestClass", "Test.TestClass", NamedTypeKind.Class);

    protected static MethodSymbol MakeMethod(
        string name,
        TypeSymbol returnType,
        params ParameterSymbol[] extraParams)
    {
        var thisParam = new ParameterSymbol("this", TestType, 0);
        var all = new[] { thisParam }
            .Concat(extraParams)
            .ToArray();
        return new MethodSymbol(name, returnType, all);
    }

    protected static ConstructorSymbol MakeConstructor(params ParameterSymbol[] extraParams)
    {
        var thisParam = new ParameterSymbol("this", TestType, 0);
        var all = new[] { thisParam }.Concat(extraParams).ToArray();
        return new ConstructorSymbol(TestType, all);
    }

    protected static ParameterSymbol Param(string name, TypeSymbol type, int ordinal)
        => new(name, type, ordinal);

    protected static LocalSymbol Local(string name, TypeSymbol type)
        => new(name, type);

    // Common assertion helpers
    protected static void AssertNoErrors(DiagnosticBag db)
        => Assert.That(db.Items.Where(d => d.Severity == Severity.Error), Is.Empty,
            $"Expected no errors but got: {string.Join(", ", db.Items.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");

    protected static void AssertNoWarnings(DiagnosticBag db)
        => Assert.That(db.Items.Where(d => d.Severity == Severity.Warning), Is.Empty,
            $"Expected no warnings but got: {string.Join(", ", db.Items.Where(d => d.Severity == Severity.Warning).Select(d => d.Message))}");

    protected static void AssertError(DiagnosticBag db, ErrorCode code)
        => Assert.That(db.Items, Has.Some.Matches<Diagnostic>(d =>
            d.Severity == Severity.Error && d.Code == code),
            $"Expected error {code.ToCodeString()} but it was not reported.");

    protected static void AssertWarning(DiagnosticBag db, ErrorCode code)
        => Assert.That(db.Items, Has.Some.Matches<Diagnostic>(d =>
            d.Severity == Severity.Warning && d.Code == code),
            $"Expected warning {code.ToCodeString()} but it was not reported.");

    protected static void AssertWarningCount(DiagnosticBag db, int count)
        => Assert.That(db.Items.Count(d => d.Severity == Severity.Warning), Is.EqualTo(count));

    protected static void AssertErrorCount(DiagnosticBag db, int count)
        => Assert.That(db.Items.Count(d => d.Severity == Severity.Error), Is.EqualTo(count));
}