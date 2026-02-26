using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public sealed class WhileStatementNode(
    IExpressionNode condition,
    IStatementNode body,
    SourceSpan span) : AstNodeBase, IStatementNode
{
    public IExpressionNode Condition => condition;
    public IStatementNode Body => body;
    public override SourceSpan Span => span;

    public override string ToPrintable()
    {
        return $"while ({Condition}) {{\n{Body}\n}}";
    }
}