using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Statements;

public class BoundObjectAllocationStatement(SourceSpan span, LocalSymbol target, NamedTypeSymbol type) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.ObjectAllocationStatement;
    public LocalSymbol Target { get; } = target;
    public NamedTypeSymbol Type { get; } = type;
}