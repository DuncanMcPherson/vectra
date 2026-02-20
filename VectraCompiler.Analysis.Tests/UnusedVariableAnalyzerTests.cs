using VectraCompiler.Analysis.Analyzers;
using VectraCompiler.Analysis.Tests.Utilities;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;
using static VectraCompiler.Analysis.Tests.Utilities.BoundTreeBuilder;

namespace VectraCompiler.Analysis.Tests;

[TestFixture]
public class UnusedVariableAnalyzerTests : AnalyzerTestBase
{
    private readonly UnusedVariableAnalyzer _sut = new();

    [Test]
    public void UsedLocal_NoWarning()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var local = Local("x", BuiltInTypeSymbol.Number);
        var body = Block(
            VarDecl(local, NumberLit(1)),
            Return(ReadLocal(local)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoWarnings(db);
    }

    [Test]
    public void UnusedLocal_ReportsWarning()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var local = Local("x", BuiltInTypeSymbol.Number);
        var body = Block(VarDecl(local, NumberLit(1)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertWarning(db, ErrorCode.UnusedVariable);
    }

    [Test]
    public void WriteOnlyLocal_ReportsWarning()
    {
        // Assigned but never read — still unused
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var local = Local("x", BuiltInTypeSymbol.Number);
        var localExpr = ReadLocal(local);
        var body = Block(
            VarDecl(local, NumberLit(1)),
            ExprStmt(Assign(localExpr, NumberLit(2))));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertWarning(db, ErrorCode.UnusedVariable);
    }

    [Test]
    public void FieldRead_NoWarning()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var field = new FieldSymbol("Score", BuiltInTypeSymbol.Number, TestType);
        var body = Block(ExprStmt(ReadLocal(field)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoWarnings(db);
    }
}