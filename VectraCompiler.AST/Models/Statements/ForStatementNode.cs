using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public sealed class ForStatementNode(
    IStatementNode? initializer,
    IExpressionNode? condition,
    IExpressionNode? increment,
    IStatementNode body,
    SourceSpan span) : AstNodeBase, IStatementNode
{
    public IStatementNode? Initializer => initializer;
    public IExpressionNode? Condition => condition;
    public IExpressionNode? Increment => increment;
    public IStatementNode Body => body;
    public override SourceSpan Span => span;
    
    public override string ToPrintable()
    {
        return $"for ({Initializer}; {Condition}; {Increment}) {{\n{Body}\n}}";
    }
}