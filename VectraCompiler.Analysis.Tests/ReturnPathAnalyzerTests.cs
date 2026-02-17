using VectraCompiler.Analysis.Analyzers;
using VectraCompiler.Analysis.Tests.Utilities;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;
using static VectraCompiler.Analysis.Tests.Utilities.BoundTreeBuilder;

namespace VectraCompiler.Analysis.Tests;

[TestFixture]
public sealed class ReturnPathAnalyzerTests : AnalyzerTestBase
{
    private readonly ReturnPathAnalyzer _sut = new();

    [Test]
    public void VoidMethod_NoReturn_NoError()
    {
        var method = MakeMethod("DoThing", BuiltInTypeSymbol.Void);
        var body = Block(ExprStmt(NumberLit(1)));
        var db = new DiagnosticBag();
        _sut.Analyze(method, body, db);
        AssertNoErrors(db);
    }

    [Test]
    public void Constructor_NoReturn_NoError()
    {
        var ctor = MakeConstructor();
        var body = Block();
        var db = new DiagnosticBag();

        _sut.Analyze(ctor, body, db);

        AssertNoErrors(db);
    }

    [Test]
    public void NonVoidMethod_WithReturn_NoError()
    {
        var method = MakeMethod("GetValue", BuiltInTypeSymbol.Number);
        var body = Block(Return(NumberLit(42)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoErrors(db);
    }

    [Test]
    public void NonVoidMethod_MissingReturn_ReportsError()
    {
        var method = MakeMethod("GetValue", BuiltInTypeSymbol.Number);
        var body = Block(ExprStmt(NumberLit(1)));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertError(db, ErrorCode.MissingReturnPath);
    }

    [Test]
    public void NonVoidMethod_EmptyBody_ReportsError()
    {
        var method = MakeMethod("GetValue", BuiltInTypeSymbol.Number);
        var body = Block();
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertError(db, ErrorCode.MissingReturnPath);
    }

    [Test]
    public void NonVoidMethod_ReturnInNestedBlock_NoError()
    {
        var method = MakeMethod("GetValue", BuiltInTypeSymbol.Number);
        var body = Block(Block(Return(NumberLit(1))));
        var db = new DiagnosticBag();

        _sut.Analyze(method, body, db);

        AssertNoErrors(db);
    }
}