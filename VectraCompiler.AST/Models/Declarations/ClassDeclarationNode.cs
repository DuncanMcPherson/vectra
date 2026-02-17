using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Declarations;

public class ClassDeclarationNode(string name, IList<IMemberNode> members, SourceSpan span) : AstNodeBase, ITypeDeclarationNode
{
    public string Name { get; } = name;
    public IList<IMemberNode> Members { get; } = members;
    public override SourceSpan Span { get; } = span;

    public override T Visit<T>(IAstVisitor<T> visitor) => visitor.VisitClassDeclaration(this);

    public override string ToPrintable()
    {
        return $"class {Name} {{\n\t{string.Join("\n\n\t", Members)}\n}}";
    }
}