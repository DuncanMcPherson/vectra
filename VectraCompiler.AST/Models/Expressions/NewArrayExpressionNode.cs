using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public sealed class NewArrayExpressionNode(
    string elementTypeName,
    IExpressionNode countExpression,
    SourceSpan span) : AstNodeBase, IExpressionNode
{
    public string ElementTypeName { get; } = elementTypeName;
    public IExpressionNode CountExpression { get; } = countExpression;
    public override SourceSpan Span { get; } = span;

    public override string ToPrintable()
    {
        return $"new {ElementTypeName}[{CountExpression}]";
    }
}