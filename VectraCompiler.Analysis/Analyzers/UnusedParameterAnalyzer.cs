using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Analysis.Analyzers;

public sealed class UnusedParameterAnalyzer : IAnalyzer
{
    public void Analyze(CallableSymbol callable, BoundBlockStatement body, DiagnosticBag diagnostics)
    {
        var candidates = callable.Parameters
            .Where(p => p.Name != "this")
            .ToHashSet();
        if (candidates.Count == 0)
            return;

        var read = new HashSet<VariableSymbol>();
        CollectFromBlock(body, read);

        foreach (var param in candidates)
        {
            if (!read.Contains(param))
            {
                diagnostics.Warning(ErrorCode.UnusedParameter, $"Parameter '{param.Name}' is never used", body.Span);
            }
        }
    }

    private static void CollectFromBlock(BoundBlockStatement block, HashSet<VariableSymbol> read)
    {
        foreach (var stmt in block.Statements)
            CollectFromStatement(stmt, read);
    }

    private static void CollectFromStatement(BoundStatement stmt, HashSet<VariableSymbol> read)
    {
        switch (stmt)
        {
            case BoundVariableDeclarationStatement decl:
                if (decl.Initializer is not null)
                    CollectFromExpression(decl.Initializer, read);
                break;
            case BoundExpressionStatement expr:
                CollectFromExpression(expr.Expression, read);
                break;
            case BoundReturnStatement ret:
                if (ret.Expression is not null)
                    CollectFromExpression(ret.Expression, read);
                break;
            case BoundBlockStatement nested:
                CollectFromBlock(nested, read);
                break;
        }
    }

    private static void CollectFromExpression(BoundExpression expr, HashSet<VariableSymbol> read)
    {
        switch (expr)
        {
            case BoundLocalExpression { Local: ParameterSymbol } param:
                read.Add(param.Local);
                break;
            case BoundAssignmentExpression assignment:
                CollectFromExpression(assignment.Value, read);
                break;
            case BoundBinaryExpression binary:
                CollectFromExpression(binary.Left, read);
                CollectFromExpression(binary.Right, read);
                break;
            case BoundCallExpression call:
                if (call.Receiver is not null)
                    CollectFromExpression(call.Receiver, read);
                foreach (var arg in call.Arguments)
                    CollectFromExpression(arg, read);
                break;
        }
    }
}