using VectraCompiler.AST.Models;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundBlockStatement(SourceSpan span, IReadOnlyList<BoundStatement> statements) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.BlockStatement;
    public IReadOnlyList<BoundStatement> Statements { get; } = statements.ToArray();
}