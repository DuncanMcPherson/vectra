using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public sealed class ThrowStatementNode(
    IExpressionNode expression,
    SourceSpan span) : IStatementNode
{
    public IExpressionNode Expression { get; } = expression;
    public SourceSpan Span { get; } = span;

    public string ToPrintable()
    {
        return $"throw {Expression};";
    }
}