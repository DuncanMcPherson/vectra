namespace VectraCompiler.Bind.Bodies;

public enum BoundNodeKind
{
    BlockStatement,
    ExpressionStatement,
    ReturnStatement,
    VariableDeclarationStatement,
    LiteralExpression,
    LocalExpression,
    AssignmentExpression,
    BinaryExpression,
    CallExpression,
    MemberAccessExpression,
    NewExpression
}