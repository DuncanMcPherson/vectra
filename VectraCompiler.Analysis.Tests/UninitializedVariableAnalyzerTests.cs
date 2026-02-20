using VectraCompiler.Analysis.Analyzers;
using VectraCompiler.Analysis.Tests.Utilities;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;
using static VectraCompiler.Analysis.Tests.Utilities.BoundTreeBuilder;

namespace VectraCompiler.Analysis.Tests;

[TestFixture]
public class UninitializedVariableAnalyzerTests : AnalyzerTestBase
{
    private readonly UninitializedVariableAnalyzer _sut = new();

    [Test]
    public void ReadInitializedLocal_NoError()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var local = Local("x", BuiltInTypeSymbol.Number);
        var body = Block(
            VarDecl(local, NumberLit(1)),
            ExprStmt(ReadLocal(local)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoErrors(db);
    }

    [Test]
    public void ReadUninitializedLocal_ReportsError()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var local = Local("x", BuiltInTypeSymbol.Number);
        var body = Block(
            VarDecl(local),          // declared with no initializer
            ExprStmt(ReadLocal(local)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertError(db, ErrorCode.UseOfUninitializedVariable);
    }

    [Test]
    public void ReadParameter_NoError()
    {
        var param = Param("value", BuiltInTypeSymbol.Number, 1);
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void, param);
        var body = Block(ExprStmt(ReadLocal(param)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoErrors(db);
    }

    [Test]
    public void ReadFieldOrProperty_NoError()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var field = new FieldSymbol("Score", BuiltInTypeSymbol.Number, TestType);
        var body = Block(ExprStmt(ReadLocal(field)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoErrors(db);
    }

    [Test]
    public void AssignThenRead_NoError()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var local = Local("x", BuiltInTypeSymbol.Number);
        var localExpr = ReadLocal(local);
        var body = Block(
            VarDecl(local),
            ExprStmt(Assign(localExpr, NumberLit(5))),
            ExprStmt(ReadLocal(local)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoErrors(db);
    }
}