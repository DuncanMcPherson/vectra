using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public sealed class IfStatementNode(
    IExpressionNode condition,
    IStatementNode thenBranch,
    IStatementNode? elseBranch,
    SourceSpan span) : AstNodeBase, IStatementNode
{
    public IExpressionNode Condition => condition;
    public IStatementNode ThenBranch => thenBranch;
    public IStatementNode? ElseBranch => elseBranch;
    public override SourceSpan Span => span;

    public override string ToPrintable()
    {
        return $"if ({Condition}) {{\n{ThenBranch}\n}}{(ElseBranch is null ? "" : $"else {{\n{ElseBranch}\n}}")}";
    }
}