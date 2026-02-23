using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundTryStatement(
    SourceSpan span,
    BoundBlockStatement tryBlock,
    BoundCatchClause? catchClause,
    BoundBlockStatement? finallyBlock) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.TryStatement;
    public BoundBlockStatement TryBlock { get; } = tryBlock;
    public BoundCatchClause? CatchClause { get; } = catchClause;
    public BoundBlockStatement? FinallyBlock { get; } = finallyBlock;
}

public sealed class BoundCatchClause(
    SourceSpan span,
    LocalSymbol? exceptionLocal,
    BoundBlockStatement body)
{
    public SourceSpan Span { get; } = span;
    public LocalSymbol? ExceptionLocal { get; } = exceptionLocal;
    public BoundBlockStatement Body { get; } = body;
}