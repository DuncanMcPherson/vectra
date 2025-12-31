using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.AST.Models.Statements;

namespace VectraCompiler.AST.Models.Declarations;

public class ConstructorDeclarationNode (string name, IList<VParameter> parameters, IList<IStatementNode> body, SourceSpan span) : CallableMember
{
    public override string Name { get; } = name;
    public override IList<VParameter> Parameters { get; } = parameters;
    public override IList<IStatementNode> Body { get; } = body;
    public override SourceSpan Span { get; } = span;
    public override T Visit<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitConstructorDeclaration(this);
    }
}