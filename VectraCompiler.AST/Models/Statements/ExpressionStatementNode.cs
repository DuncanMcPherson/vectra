using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public class ExpressionStatementNode(IExpressionNode expression, SourceSpan span) : AstNodeBase, IStatementNode
{
    public IExpressionNode Expression { get; } = expression;
    public override SourceSpan Span { get; } = span;
    
    public override T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitExpressionStatement(this);

    public override string ToPrintable()
    {
        return $"{Expression};";
    }
}