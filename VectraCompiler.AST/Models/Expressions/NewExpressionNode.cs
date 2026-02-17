using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public class NewExpressionNode(string typeName, IList<IExpressionNode> arguments, SourceSpan span) : AstNodeBase, IExpressionNode
{
    public string TypeName { get; } = typeName;
    public IList<IExpressionNode> Arguments { get; } = arguments;
    public override SourceSpan Span { get; } = span;

    public override T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitNewExpression(this);
    public override string ToPrintable() => $"new {TypeName}({string.Join(", ", Arguments)})";
}