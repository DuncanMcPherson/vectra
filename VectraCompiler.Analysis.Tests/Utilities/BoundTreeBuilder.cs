using VectraCompiler.Bind.Bodies;
using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Analysis.Tests.Utilities;

public static class BoundTreeBuilder
{
    public static readonly SourceSpan Dummy = new(
        new TokenPosition(1, 1),
        new TokenPosition(1, 1));

    public static BoundBlockStatement Block(params BoundStatement[] stmts)
        => new(Dummy, stmts);

    public static BoundReturnStatement Return(BoundExpression? value = null)
        => new(Dummy, value);

    public static BoundExpressionStatement ExprStmt(BoundExpression expr)
        => new(Dummy, expr);

    public static BoundVariableDeclarationStatement VarDecl(LocalSymbol local, BoundExpression? init = null)
        => new(Dummy, local, init);

    public static BoundLiteralExpression NumberLit(double value)
        => new(Dummy, value, BuiltInTypeSymbol.Number);

    public static BoundLiteralExpression StringLit(string value)
        => new(Dummy, value, BuiltInTypeSymbol.String);

    public static BoundLiteralExpression BoolLit(bool value)
        => new(Dummy, value, BuiltInTypeSymbol.Bool);

    public static BoundLocalExpression ReadLocal(VariableSymbol symbol)
        => new(Dummy, symbol);

    public static BoundAssignmentExpression Assign(BoundExpression target, BoundExpression value)
        => new(Dummy, target, value);

    public static BoundBinaryExpression Binary(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        => new(Dummy, left, op, right);
}