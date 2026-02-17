using VectraCompiler.Analysis.Analyzers;
using VectraCompiler.Analysis.Tests.Utilities;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;
using static VectraCompiler.Analysis.Tests.Utilities.BoundTreeBuilder;

namespace VectraCompiler.Analysis.Tests;

[TestFixture]
public class UnusedParameterAnalyzerTests : AnalyzerTestBase
{
    private readonly UnusedParameterAnalyzer _sut = new();

    [Test]
    public void UsedParameter_NoWarning()
    {
        var param = Param("player", new NamedTypeSymbol("Player", "Test.Player", NamedTypeKind.Class), 1);
        var method = MakeMethod("Observe", BuiltInTypeSymbol.Void, param);
        var body = Block(ExprStmt(ReadLocal(param)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoWarnings(db);
    }

    [Test]
    public void UnusedParameter_ReportsWarning()
    {
        var param = Param("player", new NamedTypeSymbol("Player", "Test.Player", NamedTypeKind.Class), 1);
        var method = MakeMethod("Observe", BuiltInTypeSymbol.Void, param);
        var body = Block();
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertWarning(db, ErrorCode.UnusedParameter);
    }

    [Test]
    public void ThisParameter_NeverWarns()
    {
        // MakeMethod always injects 'this' — an empty body should not warn about it
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var body = Block();
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoWarnings(db);
    }

    [Test]
    public void NoParameters_NoWarning()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var body = Block();
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoWarnings(db);
    }

    [Test]
    public void MultipleParams_OnlyUnusedWarns()
    {
        var usedParam = Param("name", BuiltInTypeSymbol.String, 1);
        var unusedParam = Param("unused", BuiltInTypeSymbol.Number, 2);
        var method = MakeMethod("Greet", BuiltInTypeSymbol.Void, usedParam, unusedParam);
        var body = Block(ExprStmt(ReadLocal(usedParam)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertWarningCount(db, 1);
        AssertWarning(db, ErrorCode.UnusedParameter);
    }
}