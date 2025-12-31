using VectraCompiler.AST.Models.Statements;
namespace VectraCompiler.AST.Models.Declarations.Interfaces;

public abstract class CallableMember : IMemberNode
{
    public abstract string Name { get; }
    public abstract IList<VParameter> Parameters { get; }
    public abstract IList<IStatementNode> Body { get; }
    public abstract SourceSpan Span { get; }
    public abstract T Visit<T>(IAstVisitor<T> visitor);
}