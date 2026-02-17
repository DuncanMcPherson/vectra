using System.Collections.Immutable;
using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public class BoundMethodGroupExpression(SourceSpan span, BoundExpression receiver, ImmutableArray<MethodSymbol> candidates)
: BoundExpression(span, receiver.Type)
{
    public override BoundNodeKind Kind => BoundNodeKind.MethodGroupExpression;
    public BoundExpression Receiver { get; } = receiver;
    public ImmutableArray<MethodSymbol> Candidates { get; } = candidates;
}