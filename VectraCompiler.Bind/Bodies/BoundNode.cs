using VectraCompiler.AST.Models;

namespace VectraCompiler.Bind.Bodies;

public abstract class BoundNode(SourceSpan span)
{
    public SourceSpan Span { get; } = span;
    public abstract BoundNodeKind Kind { get; }
}