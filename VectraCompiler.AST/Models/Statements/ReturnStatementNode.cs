using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public class ReturnStatementNode(IExpressionNode? value, SourceSpan span) : AstNodeBase, IStatementNode
{
    public IExpressionNode? Value { get; } = value;
    public override SourceSpan Span { get; } = span;

    public ReturnStatementNode(SourceSpan span) : this(null, span) { }

    public override string ToPrintable() => $"return {Value};";
}