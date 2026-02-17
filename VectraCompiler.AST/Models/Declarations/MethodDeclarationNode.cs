using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.AST.Models.Statements;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Declarations;

public class MethodDeclarationNode(string name, IList<VParameter> parameters, BlockStatementNode body, string returnType, SourceSpan span) : CallableMember
{
    public override string Name { get; } = name;
    public override IList<VParameter> Parameters { get; } = parameters;
    public override BlockStatementNode Body { get; } = body;
    public override SourceSpan Span { get; } = span;
    public string ReturnType { get; } = returnType;
    public override T Visit<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitMethodDeclaration(this);
    }

    public override string ToPrintable()
    {
        return $"{ReturnType} {Name}({string.Join(", ", Parameters)}) {{\n\t{string.Join("\n\t", Body)}\n}}";
    }
}