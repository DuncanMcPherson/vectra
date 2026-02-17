using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Expressions;

public class BoundMemberAccessExpressionReceiver(SourceSpan span, BoundExpression receiver, VariableSymbol member)
: BoundExpression(span, member.Type)
{
    public override BoundNodeKind Kind => BoundNodeKind.MemberAccessExpression;
    public BoundExpression Receiver { get; } = receiver;
    public VariableSymbol Member { get; } = member;
}