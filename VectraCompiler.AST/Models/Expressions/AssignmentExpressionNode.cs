namespace VectraCompiler.AST.Models.Expressions;

public class AssignmentExpressionNode : AstNodeBase, IExpressionNode
{
    public IExpressionNode Target { get; }
    public IExpressionNode Right { get; }

    public AssignmentExpressionNode(IExpressionNode target, IExpressionNode right, SourceSpan location)
    {
        Target = target;
        Right = right;
        Span = location;
    }

    public override SourceSpan Span { get; }
    public override T Visit<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitAssignmentExpression(this);
    }

    public override string ToPrintable()
    {
        return $"{Target} = {Right}";
    }
}