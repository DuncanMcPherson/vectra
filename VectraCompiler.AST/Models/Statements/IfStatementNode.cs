using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public sealed class IfStatementNode(
    IExpressionNode condition,
    IStatementNode thenBranch,
    IStatementNode? elseBranch,
    SourceSpan span) : IStatementNode
{
    public IExpressionNode Condition => condition;
    public IStatementNode ThenBranch => thenBranch;
    public IStatementNode? ElseBranch => elseBranch;
    public SourceSpan Span => span;

    public string ToPrintable()
    {
        return $"if ({Condition}) {{\n{ThenBranch}\n}}{(ElseBranch is null ? "" : $"else {{\n{ElseBranch}\n}}")}";
    }
}