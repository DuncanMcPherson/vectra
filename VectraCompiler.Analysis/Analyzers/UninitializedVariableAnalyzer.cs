using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Analysis.Analyzers;

public sealed class UninitializedVariableAnalyzer : IAnalyzer
{
    public void Analyze(CallableSymbol callable, BoundBlockStatement body, DiagnosticBag diagnostics)
    {
        var assigned = new HashSet<VariableSymbol>(callable.Parameters);
        CheckBlock(body, assigned, diagnostics);
    }

    private static void CheckBlock(BoundBlockStatement block, HashSet<VariableSymbol> assigned,
        DiagnosticBag diagnostics)
    {
        foreach (var stmt in block.Statements)
            CheckStatement(stmt, assigned, diagnostics);
    }
    
    private static void CheckStatement(BoundStatement stmt, HashSet<VariableSymbol> assigned,
        DiagnosticBag diagnostics)
    {
        switch (stmt)
        {
            case BoundVariableDeclarationStatement decl:
                if (decl.Initializer is not null)
                {
                    CheckExpression(decl.Initializer, assigned, diagnostics);
                    assigned.Add(decl.Local);
                }

                break;
            case BoundExpressionStatement exprStmt:
                CheckExpression(exprStmt.Expression, assigned, diagnostics);
                break;
            case BoundReturnStatement ret:
                if (ret.Expression is not null)
                    CheckExpression(ret.Expression, assigned, diagnostics);
                break;
            case BoundBlockStatement nested:
                CheckBlock(nested, assigned, diagnostics);
                break;
        }
    }

    private static void CheckExpression(BoundExpression expr, HashSet<VariableSymbol> assigned,
        DiagnosticBag diagnostics)
    {
        switch (expr)
        {
            case BoundLocalExpression { Local: not (FieldSymbol or PropertySymbol) } local:
                if (!assigned.Contains(local.Local))
                    diagnostics.Error(
                        ErrorCode.UseOfUninitializedVariable,
                        $"Variable '{local.Local.Name}' is used before it has been assigned.", local.Span);
                break;
            case BoundAssignmentExpression assignment:
                CheckExpression(assignment.Value, assigned, diagnostics);
                if (assignment.Target is BoundLocalExpression targetLocal)
                    assigned.Add(targetLocal.Local);
                break;
            case BoundBinaryExpression binary:
                CheckExpression(binary.Left, assigned, diagnostics);
                CheckExpression(binary.Right, assigned, diagnostics);
                break;

            case BoundCallExpression call:
                if (call.Receiver is not null)
                    CheckExpression(call.Receiver, assigned, diagnostics);
                foreach (var arg in call.Arguments)
                    CheckExpression(arg, assigned, diagnostics);
                break;
        }
    }
}