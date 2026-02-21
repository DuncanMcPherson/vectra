using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public class BlockStatementNode(SourceSpan span) : AstNodeBase, IStatementNode
{
    public required IReadOnlyList<IStatementNode> Statements { get; init; }
    public override SourceSpan Span { get; } = span;
    

    public override string ToPrintable()
    {
        return string.Join('\n', Statements);
    }
}