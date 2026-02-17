using VectraCompiler.Analysis.Analyzers;
using VectraCompiler.Analysis.Tests.Utilities;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;
using static VectraCompiler.Analysis.Tests.Utilities.BoundTreeBuilder;

namespace VectraCompiler.Analysis.Tests;

[TestFixture]
public class UnreachableCodeAnalyzerTests : AnalyzerTestBase
{
    private readonly UnreachableCodeAnalyzer _sut = new();

    [Test]
    public void NoReturnInBlock_NoWarning()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var body = Block(ExprStmt(NumberLit(1)), ExprStmt(NumberLit(2)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoWarnings(db);
    }

    [Test]
    public void ReturnAtEnd_NoWarning()
    {
        var method = MakeMethod("GetValue", BuiltInTypeSymbol.Number);
        var body = Block(ExprStmt(NumberLit(1)), Return(NumberLit(42)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoWarnings(db);
    }

    [Test]
    public void StatementAfterReturn_ReportsWarning()
    {
        var method = MakeMethod("GetValue", BuiltInTypeSymbol.Number);
        var body = Block(Return(NumberLit(1)), ExprStmt(NumberLit(2)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertWarning(db, ErrorCode.UnreachableCode);
    }

    [Test]
    public void MultipleStatementsAfterReturn_ReportsOnlyOneWarning()
    {
        var method = MakeMethod("GetValue", BuiltInTypeSymbol.Number);
        var body = Block(Return(NumberLit(1)), ExprStmt(NumberLit(2)), ExprStmt(NumberLit(3)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertWarningCount(db, 1);
    }
}