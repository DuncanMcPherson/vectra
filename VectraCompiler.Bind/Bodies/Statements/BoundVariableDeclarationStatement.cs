using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Bodies.Expressions;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Bodies.Statements;

public sealed class BoundVariableDeclarationStatement(SourceSpan span, LocalSymbol local, BoundExpression? initializer) : BoundStatement(span)
{
    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;
    public LocalSymbol Local { get; } = local;
    public BoundExpression? Initializer { get; } = initializer;
}