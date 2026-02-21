using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public sealed class TryStatementNode(
    BlockStatementNode tryBlock,
    CatchClauseNode? catchClause,
    BlockStatementNode? finallyBlock,
    SourceSpan span) : IStatementNode
{
    public BlockStatementNode TryBlock { get; } = tryBlock;
    public CatchClauseNode? CatchClause { get; } = catchClause;
    public BlockStatementNode? FinallyBlock { get; } = finallyBlock;
    public SourceSpan Span { get; } = span;

    public string ToPrintable()
    {
        return
            $"try {{\n{TryBlock}\n}}{(CatchClause == null ? "" : $"\n{CatchClause}")}{(FinallyBlock == null ? "" : $"\nfinally {{\n{FinallyBlock}\n}}")}";
    }
}

public sealed class CatchClauseNode(
    string? exceptionType,
    string? exceptionName,
    BlockStatementNode body,
    SourceSpan span)
{
    public string? ExceptionType { get; } = exceptionType;
    public string? ExceptionName { get; } = exceptionName;
    public BlockStatementNode Body { get; } = body;
    public SourceSpan Span { get; } = span;

    public override string ToString()
    {
        return $"catch {(ExceptionName == null ? "" : $"({ExceptionType} {ExceptionName}) ")}{{\n{Body}\n}}";
    }
}