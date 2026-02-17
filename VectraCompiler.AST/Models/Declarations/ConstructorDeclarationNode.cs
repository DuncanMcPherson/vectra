using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.AST.Models.Statements;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Declarations;

public class ConstructorDeclarationNode (string name, IList<VParameter> parameters, BlockStatementNode body, SourceSpan span) : CallableMember
{
    public override string Name { get; } = name;
    public override IList<VParameter> Parameters { get; } = parameters;
    public override BlockStatementNode Body { get; } = body;
    public override SourceSpan Span { get; } = span;
    public override T Visit<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitConstructorDeclaration(this);
    }

    public override string ToPrintable()
    {
        return $"{Name}({string.Join(", ", Parameters)}) {{\n\t{string.Join("\n\t", Body)}\n}}";
    }
}