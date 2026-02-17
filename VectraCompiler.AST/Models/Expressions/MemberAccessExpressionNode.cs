using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Expressions;

public class MemberAccessExpressionNode : AstNodeBase, IExpressionNode
{
    public IExpressionNode Object { get; }
    public string TargetName { get; }
    public override SourceSpan Span { get; }

    public MemberAccessExpressionNode(IExpressionNode objectNode, string targetName, SourceSpan span)
    {
        Object = objectNode;
        TargetName = targetName;
        Span = span;
    }
    public override T Visit<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitMemberAccessExpression(this);
    }

    public override string ToPrintable()
    {
        return $"{Object}.{TargetName}";
    }
}