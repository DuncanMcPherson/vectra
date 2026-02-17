using VectraCompiler.AST.Models;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies;

public abstract class BoundNode(SourceSpan span)
{
    public SourceSpan Span { get; } = span;
    public abstract BoundNodeKind Kind { get; }
}