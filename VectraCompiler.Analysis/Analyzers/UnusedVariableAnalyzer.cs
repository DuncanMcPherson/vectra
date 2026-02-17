using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Analysis.Analyzers;

public sealed class UnusedVariableAnalyzer : IAnalyzer
{
    public void Analyze(CallableSymbol callable, BoundBlockStatement body, DiagnosticBag diagnostics)
    {
        var declared = new Dictionary<LocalSymbol, BoundVariableDeclarationStatement>();
        var read = new HashSet<VariableSymbol>();

        CollectFromBlock(body, declared, read);

        foreach (var (local, value) in declared)
        {
            if (!read.Contains(local))
                diagnostics.Warning(
                    ErrorCode.UnusedVariable,
                    $"Variable '{local.Name}' is declared but never used.", span: value.Span);
        }
    }

    private static void CollectFromBlock(
        BoundBlockStatement block,
        Dictionary<LocalSymbol, BoundVariableDeclarationStatement> declared,
        HashSet<VariableSymbol> read)
    {
        foreach (var stmt in block.Statements)
            CollectFromStatement(stmt, declared, read);
    }

    private static void CollectFromStatement(
        BoundStatement stmt,
        Dictionary<LocalSymbol, BoundVariableDeclarationStatement> declared,
        HashSet<VariableSymbol> read)
    {
        switch (stmt)
        {
            case BoundVariableDeclarationStatement decl:
                declared[decl.Local] = decl;
                if (decl.Initializer is not null)
                    CollectFromExpression(decl.Initializer, declared, read);
                break;

            case BoundExpressionStatement exprStmt:
                CollectFromExpression(exprStmt.Expression, declared, read);
                break;

            case BoundReturnStatement ret:
                if (ret.Expression is not null)
                    CollectFromExpression(ret.Expression, declared, read);
                break;

            case BoundBlockStatement nested:
                CollectFromBlock(nested, declared, read);
                break;
        }
    }

    private static void CollectFromExpression(
        BoundExpression expr,
        Dictionary<LocalSymbol, BoundVariableDeclarationStatement> declared,
        HashSet<VariableSymbol> read)
    {
        switch (expr)
        {
            case BoundLocalExpression { Local: not FieldSymbol or PropertySymbol } local:
                read.Add(local.Local);
                break;

            case BoundAssignmentExpression assignment:
                // Writing to a local is NOT a read; only the RHS counts
                CollectFromExpression(assignment.Value, declared, read);
                break;

            case BoundBinaryExpression binary:
                CollectFromExpression(binary.Left, declared, read);
                CollectFromExpression(binary.Right, declared, read);
                break;

            case BoundCallExpression call:
                if (call.Receiver is not null)
                    CollectFromExpression(call.Receiver, declared, read);
                foreach (var arg in call.Arguments)
                    CollectFromExpression(arg, declared, read);
                break;
        }
    }
}